<h1 align="center">Jellyfin Kodi Sync Queue Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org">Jellyfin Project</a></h3>

## About

This plugin will track all media changes while any Kodi clients are offline to decrease sync times when logging back in to your server.

## Build & Installation Process

1. Clone this repository

2. Ensure you have .NET Core SDK set up and installed

3. Build the plugin with your favorite IDE or the `dotnet` command

```sh
dotnet publish --configuration Release --output bin
```

4. Place the resulting file in a folder called `plugins` in the data directory
