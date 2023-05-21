# kosync-dotnet

**kosync-dotnet** is a self-hostable implementation of the KOReader sync server built with .NET. It aims to extend the existing functionality of the official [koreader-sync-server](https://github.com/koreader/koreader-sync-server).

Users of KOReader can register an account on this synchronisation server and use the inbuilt *Progress sync* plugin to keep all reading progress synchronised between devices.

All data is stored inside a [LiteDB](https://www.litedb.org/) database file.