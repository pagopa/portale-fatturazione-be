
# the app service to deploy
data "azurerm_linux_web_app" "app" {
  name                = var.app_name
  resource_group_name = var.app_resource_group_name
}
