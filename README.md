# Jellyfin Plugin Wholphin

> [!WARNING]
> This plugin in still a work-in-progress and is very unstable!

This is a Jellyfin server plugin that provides extra functionality to [Wholphin](https://github.com/damontecres/Wholphin), a third-party Android TV client for Jellyfin.

Please note: this plugin is not required to use Wholphin.

> [!NOTE]
> Using this plugin currently requires using this [develop build of Wholphin](https://github.com/damontecres/Wholphin/releases/tag/develop-server-plugin).

## Installation

TODO

## Configuration

### Web UI config

The plugin can be configured via the web UI.

WIP

### YAML config

The plugin can be configured with YAML:

```yaml
# Version of the settings
Version: 1
# Home page config
HomeConfig:
  HomePageSettings:
    # Version of the home page settings
    Version: 1
    # Rows
    Rows:
      # A row for next up items
      - type: NextUp
      # A row for a collection (parent) sorted randomly
      - type: ByParent
        ParentId: <UUID>
        Recursive: false
        Sort:
          Sort: Random
          Direction: Ascending
# Seerr config
SeerrConfig: {}

```

#### Home page rows

Sample YAML for home page rows

```yaml

# Continue watching row (not combined)
- type: ContinueWatching

# Next up row (not combined)
- type: NextUp

# Row of combined continue watching & next up
- type: ContinueWatchingCombined

# Recently added in a library
- type: RecentlyAdded
  parentId: <UUID> # Library UUID

# Genres in a library
- type: Genres
  parentId: <UUID> # Library UUID

# Favorite shows
- type: Favorite
  kind: Series

# Collection or playlist
- type: ByParent
  parentId: <UUID> # Collection/Playlist ID
  recursive: true
  sort: # Optional
    sort: SortName
    direction: Ascending

# Items from an arbitrary Jellyfin endpoint that returns a QueryResult<BaseItemDto>.
# Lets other plugins (e.g. jellyfin-plugin-home-sections) feed rows into Wholphin
# without Wholphin needing to know about them.
#
# `userId` is injected automatically from the currently logged-in user. You can
# override it by including a `userId` entry in `query`.
- type: CustomEndpoint
  title: My Requests                                  # Optional, falls back to endpoint
  endpoint: /HomeScreen/Section/MyJellyseerrRequests  # relative to the Jellyfin baseUrl, or a full URL
  query:                                              # Optional extra query parameters
    - key: language
      value: en
  headers:                                            # Optional extra headers (auth token is added automatically)
    - key: X-Trace
      value: wholphin

```

## Acknowledgements

- Credit to @kamilkosek: some code and inspiration are taken from their original idea for for a [Wholphin plugin](https://github.com/kamilkosek/jellyfin-plugin-wholphin)
