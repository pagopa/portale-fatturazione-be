#
# general
#
env_short      = "u"
env            = "uat"
prefix         = "fat"
location       = "italynorth"
location_short = "itn"

tags = {
  CreatedBy   = "Terraform"
  Environment = "UAT"
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
  environment = "uat"
}

#
# azure
#
identity_resource_group_name = "fat-u-identity-rg"
app_resource_group_name      = "fat-u-app-rg"
app_name                     = "fat-u-app-api"
