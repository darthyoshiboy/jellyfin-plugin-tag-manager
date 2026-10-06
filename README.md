# Jellyfin Tag Manager

A Jellyfin 10.9.0 plugin for managing tags across library content. The plugin provides a dashboard page where administrators can search items, select and apply multiple existing tags to a video, add new tags, and remove tags by clearing their toggles.

## Install from the plugin repository

Create a release by pushing a version tag:

```shell
git tag v1.0.0.0
git push origin v1.0.0.0
```

The publish workflow builds the plugin archive, creates a GitHub release, calculates its checksum, and updates `manifest.json` with the current release and the two previous releases. Jellyfin supports multiple versions in a repository manifest and installs a compatible version; the version list is not necessarily shown as a manual version picker.

After the first release, add the repository manifest to Jellyfin under **Dashboard > Plugins > Repositories**:

```text
https://raw.githubusercontent.com/darthyoshiboy/jellyfin-plugin-tag-manager/refs/heads/main/manifest.json
```

Replace the repository name or branch in that URL if the new repository uses different values.

## Local development

The project targets `net8.0` and references the Jellyfin 10.9.0 plugin packages. Restore dependencies and publish the plugin with:

```shell
dotnet restore
dotnet publish Jellyfin.Plugin.Template/Jellyfin.Plugin.Template.csproj --configuration Release
```

The plugin assembly is emitted as `Jellyfin.Plugin.TagManager.dll`. Install the published files in a folder named `Jellyfin.Plugin.TagManager` under the Jellyfin plugins directory, then restart Jellyfin.

## Supported endpoint

The plugin API is administrator-only and exposes tag lookup and item tag mutations under `/Plugins/TagManager`.

## License

See [LICENSE](LICENSE).
