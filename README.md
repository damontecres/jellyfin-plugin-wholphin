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
# Seerr config (push Seerr server URL + login to the client)
SeerrConfig:
  serverUrl: https://seerr.example.com
  login:
    type: Jellyfin     # ApiKey | Jellyfin | Local
    jellyfin:
      useCurrentUser: true

```

#### Seerr config

When set, Wholphin will automatically configure its Seerr connection on first
login using these values. If `login` is omitted, only the URL is pushed and the
user supplies authentication themselves.

Three login types are supported, matching Seerr's own auth options:

```yaml
# 1) API key — same login for everyone, no Seerr account needed per user
serverUrl: https://seerr.example.com
login:
  type: ApiKey
  apiKey: "<seerr-api-key>"

# 2) Jellyfin auth — Seerr re-uses Jellyfin credentials
serverUrl: https://seerr.example.com
login:
  type: Jellyfin
  jellyfin:
    # true: use whichever Jellyfin user is currently logged in to Wholphin.
    # Wholphin will pass the in-memory Jellyfin password through to Seerr
    # automatically on a fresh password login. On QuickConnect / token-based
    # logins the password is not available — Wholphin will pre-fill URL and
    # username, the end user has to enter the password once in the Seerr
    # settings dialog. After that, subsequent restarts re-authenticate
    # automatically using the stored credentials.
    useCurrentUser: true
    # Alternatively, hard-code a shared Jellyfin user/password for everyone
    # (ignored when useCurrentUser: true):
    username: "shared-jf-user"
    password: "..."

# 3) Local Seerr account — shared local Seerr login for everyone
serverUrl: https://seerr.example.com
login:
  type: Local
  local:
    username: "household@example.com"
    password: "..."
```

> [!WARNING]
> Passwords are stored in cleartext in the plugin's configuration file. Treat
> the Jellyfin config directory accordingly. Prefer `useCurrentUser: true` or
> `ApiKey` over storing a shared password.

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
  title: My Requests
  endpoint: /HomeScreen/Section/MyJellyseerrRequests  # relative to the Jellyfin baseUrl
  query:                                              # Optional extra query parameters
    - key: language
      value: en
  headers:                                            # Optional extra headers (auth token is added automatically)
    - key: X-Trace
      value: wholphin

```

## Acknowledgements

- Credit to @kamilkosek: some code and inspiration are taken from their original idea for for a [Wholphin plugin](https://github.com/kamilkosek/jellyfin-plugin-wholphin)
