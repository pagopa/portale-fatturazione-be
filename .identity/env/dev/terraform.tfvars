#
# general
#
env_short      = "d"
env            = "dev"
prefix         = "fat"
location       = "italynorth"
location_short = "itn"

tags = {
  CreatedBy   = "Terraform"
  Environment = "DEV"
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
  environment = "dev"
}

deployments = {
  review_required = false
}

#
# azure
#
identity_resource_group_name = "fat-d-identity-rg"
app_resource_group_name      = "fat-d-app-rg"
