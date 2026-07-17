# Protected Configuration Folder

This folder is monitored by the GitHub Actions `ProtectedConfigCheck.yml` workflow. Any changes to the contents of this
folder will prevent Pull Requests from being merged unless the PR is labeled with `request_config_change`.

## Important Notes

- Add the `request_config_change` label to your PR if you need to modify files in this folder
- Changes without this label will be automatically blocked
- This protection ensures intentional changes only

## Purpose

This folder contains locked final configuration values for various game systems, including:

- Locomotion settings
- Double jump parameters
- Jiggle effect values
- Other critical game configurations

These values are protected to prevent accidental modifications that could impact game balance or functionality.
