# kosync-dotnet

**kosync-dotnet** is a self-hostable implementation of the KOReader sync server built with .NET. It aims to extend the existing functionality of the official [koreader-sync-server](https://github.com/koreader/koreader-sync-server).

Users of KOReader can register a user on this synchronisation server and use the inbuilt _Progress sync_ plugin to keep all reading progress synchronised between devices.

All data is stored inside a [LiteDB](https://www.litedb.org/) database file.

## How to run your own server?

The recommendation is to use [Docker](https://www.docker.com/) with [Docker Compose](https://docs.docker.com/compose/).

```
version: "3"
services:
  kosync:
    container_name: kosync
    image: ghcr.io/jberlyn/kosync-dotnet:latest
    restart: unless-stopped
    volumes:
      - </path/to/data>:/app/data
    environment:
      - ASPNETCORE_HTTP_PORTS=8080
      - ADMIN_PASSWORD=<super-strong-password>
      - REGISTRATION_DISABLED=false
      - TRUSTED_PROXIES=1.2.3.4, ::1
    user: 1000:1000
```

`/app/data` will contain the database, which stores user and document progress information.

An admin user with the username `admin` will be created when the server first starts. If you provide the `ADMIN_PASSWORD` environment variable, the password for this admin user will be changed to the value provided. If you do not provide a value, the password for this user will be set to `admin`. The recommendation is to set this to a strong password generated with a password manager like [Bitwarden](https://bitwarden.com/). This admin user can be used to interact with the management API.

If the `REGISTRATION_DISABLED` environment variable is set to `true`, the sync server will respond with an `User registration is disabled` error message when trying to create a new user. This is useful if you expose your sync server to the public internet, but don't want anyone to be able to register a user. This is a feature that is not available in the official sync server.

The `TRUSTED_PROXIES` environment variable is an optional variable that defines one or more trusted proxies. It should be a comma delineated list of IPv4 or IPv6 addresses. If `TRUSTED_PROXIES` is set, when a request comes through a trusted proxy, the X-Forwarded-For header will be checked for the client's real IP address to use in logging. If `TRUSTED_PROXIES` is not set, or if a request does not come through a trusted proxy, the request's source IP address will be used for logging. Requests that do not come through a trusted proxy when `TRUSTED_PROXIES` is set will be marked with an asterisk (*) in the logs.

Mapping user to `1000:1000` is just a way to ensure the database files are created using your own user, rather than root. If your operating system user ID is different from `1000:1000`, change it to yours.

This example doesn't expose any ports for the container, however the sync server is accessible via port `8080` inside the container. The recommendation is to expose the server via a reverse proxy such as [Nginx Proxy Manager](https://nginxproxymanager.com/).

Prior to .NET 8, the default port was `80`. This was changed for [various reasons](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port) by Microsoft. You can manually set the port using the `ASPNETCORE_HTTP_PORTS` environment variable, if this is something you would like to configure.

## Management API

There are some management API endpoints you can interact with using a tool like [Postman](https://www.postman.com/).

Only the admin user can make requests to these API endpoints, with the exception of users being allowed to query their own documents.

All requests to these API endpoints require the following headers.

```json
{
  "x-auth-user": "admin"
  "x-auth-key": "<MD5 hash of your admin password>"
}
```

Since we reuse the existing user structure used for KOReader, unfortunately we are stuck using MD5 hashes for passwords instead of something more secure.

### GET /manage/users

Returns a list of all users.

**Example Response**

```json
[
    {
        "id": 1,
        "username": "admin",
        "isAdministrator": true,
        "isActive": true,
        "documentCount": 0
    },
    {
        "id": 1,
        "username": "jberlyn",
        "isAdministrator": false,
        "isActive": true,
        "documentCount": 1
    }
]
```

### POST /manage/users

Creates a new user. This endpoint circumvents the `REGISTRATION_DISABLED` environment variable.

**Request Body**

```json
{
  "username": "jberlyn",
  "password": "super-strong-password"
}
```

**Example Response**

```json
{
  "message": "User created successfully"
}
```

### GET /manage/users/documents?username=username

Returns a list of documents for a user and their sync status. The username for the user must be passed via a query parameter.

**Example Response**

```json
[
    {
        "documentHash": "b8a9c5b494e7c91ece4cb8407e746ec7",
        "progress": "/body/DocFragment[28]/body/div/section/h1[1]/a/span/text().0",
        "percentage": 0.3186,
        "device": "Kobo_nova",
        "deviceId": "F47D5CA7123A4ABEAB90E8BF6F836356",
        "timestamp": "2023-05-23T11:43:00.225+10:00"
    },
    {
        "documentHash": "b1e5b67d6b0fe57ce9893d08dce65406",
        "progress": "/body/DocFragment[10]/body/div/h1/text().0",
        "percentage": 0.0166,
        "device": "Kobo_nova",
        "deviceId": "F47D5CA7123A4ABEAB90E8BF6F836356",
        "timestamp": "2023-05-23T11:44:34.165+10:00"
    }
]
```

### PUT /manage/users/active?username=username

Toggles the active status of a user. The username for the user must be passed via a query parameter. Users that are marked as inactive will not be able to login or push sync progress.

**Example Response**

```json
{
    "message": "User marked as inactive"
}
```

### PUT /manage/users/password?username=username

Updates the password for a user. The username for the user must be passed via a query parameter.

**Request Body**

```json
{
  "password": "super-strong-password"
}
```

**Example Response**

```json
{
    "message": "Password changed successfully"
}
```