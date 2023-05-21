# kosync-dotnet

**kosync-dotnet** is a self-hostable implementation of the KOReader sync server built with .NET. It aims to extend the existing functionality of the official [koreader-sync-server](https://github.com/koreader/koreader-sync-server).

Users of KOReader can register an account on this synchronisation server and use the inbuilt *Progress sync* plugin to keep all reading progress synchronised between devices.

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
      - REGISTRATION_DISABLED=false
    user: 1000:1000
```

`/app/data` will contain the database, which stores user and document progress information.

If the `REGISTRATION_DISABLED` environment variable is set to `true`, the sync server will respond with an `Account registration is disabled` error message when trying to create a new account. This is useful if you expose your sync server to the public internet, but don't want anyone to be able to create an account. This is a feature that is not available in the official sync server.

Mapping user to `1000:1000` is just a way to ensure the database files are created using your own user, rather than root. If your user account ID is different from `1000:1000`, change it to yours.