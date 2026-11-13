# Jellyfin Plugin Wholphin

> [!WARNING]
> This plugin in still a work-in-progress and may be unstable!

This is a Jellyfin server plugin that provides extra functionality to [Wholphin](https://github.com/damontecres/Wholphin), a third-party Android TV client for Jellyfin.

This plugin allows for the server admin to pre-config the home page and provide Seerr integration information.

Please note: this plugin is not required to use Wholphin!

> [!NOTE]
> Wholphin `v1.1.0` or newer is required to make use of this plugin

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

# Studios in a TV show library
- type: Studios
  parentId: <UUID> # TV library UUID

# Any Jellyfin items query. `filter` is required; lists such as sortBy/sortOrder pair up by position
- type: GetItems
  name: Highly Rated
  filter:
    recursive: true
    includeItemTypes: [Movie, Series]
    minCommunityRating: 7.5
    sortBy: [CommunityRating]
    sortOrder: [Descending]

# Any row can override size and layout (values shown are the defaults)
- type: NextUp
  viewOptions:
    heightDp: 172
    spacing: 16
    aspectRatio: TALL # TALL, WIDE, FOUR_THREE, SQUARE
    imageType: PRIMARY # PRIMARY, THUMB
    contentScale: FILL # FILL, FIT, CROP, NONE, Fill_WIDTH, FILL_HEIGHT
    showTitles: false

# Items from an arbitrary Jellyfin endpoint that returns a QueryResult<BaseItemDto>.
# Lets other plugins feed rows into Wholphin without Wholphin needing to know about them,
# for example Home Screen Sections, a separate plugin that has to be installed on the server:
# https://github.com/IAmParadox27/jellyfin-plugin-home-sections
#
# `title` and `endpoint` are required; the app drops a row that has no title.
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

# With the Home Screen Sections plugin installed, it lists its sections at /HomeScreen/Sections.
# A section that takes a parameter (Genre, Studio, DirectedBy, ...) gets it as `additionalData`.
# Items are shown as the endpoint returns them, and Home Screen Sections leaves out
# descriptions, so these cards have no overview to show under the header.
- type: CustomEndpoint
  title: Action Movies
  endpoint: /HomeScreen/Section/Genre
  query:
    - key: additionalData
      value: Action

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

## Acknowledgements

- Credit to @kamilkosek: some code and inspiration are taken from their original idea for for a [Wholphin plugin](https://github.com/kamilkosek/jellyfin-plugin-wholphin)
