# infra/bootstrap

Creates the Azure Storage account that every other Terraform config in this
repo (starting with `infra/environments/dev`) uses as its remote state
backend.

This config uses **local** state on purpose: a backend must exist before
Terraform can be pointed at it as one, so this can't bootstrap itself.

## Apply once, never destroy

```bash
az login
az account list --output table          # confirm which subscription you mean
az account show --query id --output tsv # the value to pass below

cd infra/bootstrap
terraform init
terraform apply -var="subscription_id=<subscription id>"
```

`subscription_id` is required and has no default, on purpose. Without it the
provider silently uses whatever `az account show` returns — and on a machine
signed into more than one account (a personal and a work tenant, say), that
quietly puts this repo's Terraform state in the wrong subscription, with
nothing in the apply output to say so. Pass the same value you will give
`infra/environments/dev`.

Note the outputs (`resource_group_name`, `storage_account_name`,
`container_name`) — they feed the `-backend-config` values for
`infra/environments/dev` (see that environment's README).

Do not run `terraform destroy` here once any other environment has state
stored in this account — that would delete every environment's state.
