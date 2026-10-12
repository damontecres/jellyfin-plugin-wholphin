# Jellyfin Plugin Wholphin

> [!WARNING]
> This plugin in still a work-in-progress and may be unstable!

This is a Jellyfin server plugin that provides extra functionality to [Wholphin](https://github.com/damontecres/Wholphin), a third-party Android TV client for Jellyfin.

This plugin allows for the server admin to pre-config the home page and provide Seerr integration information.

Please note: this plugin is not required to use Wholphin!

> [!NOTE]
> Using this plugin currently requires using this [develop build of Wholphin](https://github.com/damontecres/Wholphin/pull/1384).

## Installation

**This plugin requires Jellyfin 12.1 or newer.**

1. Open the Jellyfin Dashboard, go to Plugins
2. Click on `Manage Repositories`
3. Click `New Repository`
4. Enter the repository URL:
   ```
   https://damontecres.github.io/Wholphin/plugin/manifest.json
   ```
5. Go back to the Plugin page and click on `Available`
6. Find `Wholphin` in the list and click on it
7. Click `Install`
8. Restart Jellyfin to complete installation (on Dashboard page)
9. Go back to Dashboard->Plugin->Wholphin
10. CLick on `Settings` to configure

## Configuration

### Web UI config

This is a work-in-progress!

### YAML config

The plugin can be configured with YAML. Only basic syntax validations are performed on the YAML input.

#### Example
```yaml
# Version of the settings
Version: 1
# Home page config
HomeConfig:
  HomePageSettings:
    # Version of the home page settings
    version: 1
    # Rows, they will appear in same order in the app
    rows:
      # A row for combined continue watching/next up items
      - type: ContinueWatchingCombined
      # A row for a collection (parent) sorted by name
      - type: ByParent
        parentId: <UUID>
        recursive: false
        sort:
          sort: SortName
          direction: Ascending
SeerrConfig:
  serverUrl: https://seerr.example.com
```

#### Home page rows

Sample YAML for home page rows

```yaml

# Continue watching row (not combined)
- type: ContinueWatching

# Next up row (not combined)
- type: NextUp

# Combined continue watching & next up
- type: ContinueWatchingCombined

# Recently added in a library or collection
- type: RecentlyAdded
  parentId: <UUID> # Library/Collection UUID

# Genres in a library
- type: Genres
  parentId: <UUID> # Library UUID

# Favorite shows
- type: Favorite
  kind: <kind> # Type of media (eg Series, Movie, Episode, Person, etc)

# Library, Collection, or playlist
- type: ByParent
  parentId: <UUID> # Library/Collection/Playlist ID
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

#### Seerr config

When set, Wholphin will automatically configure its Seerr connection on first
login using these values. If `login` is omitted, only the URL is pushed and the
user supplies authentication themselves.

Two login types are supported when credentials are provided:

```yaml
# 1) API key — same login for everyone, no Seerr account needed per user
serverUrl: https://seerr.example.com
login:
  type: ApiKey
  apiKey: "<seerr-api-key>"
# 2) Local Seerr account — shared local Seerr login for everyone
serverUrl: https://seerr.example.com
login:
  type: Local
  local:
    username: "household@example.com"
    password: "..."
```

##### Per-user Jellyfin token

A third login type, `JellyfinToken`, gives every user their own credentials without
storing any in the plugin configuration. It is designed for a Seerr-compatible proxy
that runs inside Jellyfin at `/Plugins/SeerrProxy` and accepts a Jellyfin access token
as its API key (Wholphin sends it in the `X-Api-Key` header). It does **not** work
against Seerr itself: Seerr's API key authentication does not accept Jellyfin tokens.

```yaml
serverUrl: https://jellyfin.example.com/Plugins/SeerrProxy
login:
  type: JellyfinToken
```

All clients get the same URL, but individual credentials: when Wholphin asks for the
Seerr settings, the plugin answers with an ordinary `ApiKey` login holding the
requesting user's own Jellyfin access token. No password is involved, so this also
works for users who signed in with Quick Connect.

A Jellyfin access token grants full access to the user's account (to the whole server,
for an administrator), so the plugin only provides one when:

- the request was made by a signed-in user. Server API keys are refused (403).
- `serverUrl` is exactly this Jellyfin server's `/Plugins/SeerrProxy` endpoint, at the
  address the client used to reach Jellyfin (including Jellyfin's base URL) or at the
  server's published URL. Anything else is refused (403) and a warning is logged.

Behind a reverse proxy, add the proxy to Jellyfin's Known Proxies so that Jellyfin sees
the public scheme and host of requests, or set the public address in Published Server
URIs. Both are in Jellyfin's networking settings.

Wholphin keeps the token it received when setting up Seerr. If that token is revoked or
rotated, for example by signing the device out of Jellyfin, the stored Seerr login stops
working. The plugin does not refresh it.

## Acknowledgements

- Credit to @kamilkosek: some code and inspiration are taken from their original idea for for a [Wholphin plugin](https://github.com/kamilkosek/jellyfin-plugin-wholphin)
