# Identity

Identities and environment configuration for CI/CD on GitHub.

Applying this is required for GitHub workflows defined in this repository to run.

## Notes

- The identity stack now configures deploy permissions and GitHub Environment variables for:
  - `APP_NAME_PREFIX`
- `env/dev` has been added with placeholders: replace all `TODO_*` values before applying.

<!-- markdownlint-disable -->
<!-- BEGIN_TF_DOCS -->
<!-- END_TF_DOCS -->
