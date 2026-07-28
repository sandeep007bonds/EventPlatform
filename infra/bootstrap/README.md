# infra/bootstrap

Creates the Azure Storage account that every other Terraform config in this
repo (starting with `infra/environments/dev`) uses as its remote state
backend.

This config uses **local** state on purpose: a backend must exist before
Terraform can be pointed at it as one, so this can't bootstrap itself.

## Apply once, never destroy

```bash
cd infra/bootstrap
terraform init
terraform apply
```

Note the outputs (`resource_group_name`, `storage_account_name`,
`container_name`) — they feed the `-backend-config` values for
`infra/environments/dev` (see that environment's README).

Do not run `terraform destroy` here once any other environment has state
stored in this account — that would delete every environment's state.
