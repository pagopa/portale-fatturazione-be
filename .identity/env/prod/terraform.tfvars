#
# general
#
env_short      = "p"
env            = "prod"
prefix         = "fat"
location       = "italynorth"
location_short = "itn"

tags = {
  CreatedBy   = "Terraform"
  Environment = "PROD"
  Owner       = "PagoPA ICT"
  Source      = "https://github.com/pagopa/portale-fatturazione-be"
  CostCenter  = "TS230 - PagoPA ICT"
}

#
# github
#
github = {
  org         = "pagopa"
  repository  = "portale-fatturazione-be"
  environment = "prod"
}

#
# azure
#
identity_resource_group_name = "fat-p-identity-rg"
app_resource_group_name      = "fat-p-app-rg"
app_name                     = "fat-p-app-api"
