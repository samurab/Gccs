terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.80.0"
    }
  }

  # Supply the production state account settings at init time. Local state must
  # never be used for production adoption or drift detection.
  backend "azurerm" {}
}

provider "azurerm" {
  subscription_id = var.subscription_id
  features {}
}

variable "subscription_id" {
  type        = string
  description = "Azure subscription containing the commercial production environment."
}

variable "alert_email_address" {
  type        = string
  description = "Approved production security/operations alert mailbox."
}

variable "production_service_plan_id" {
  type        = string
  description = "Existing App Service plan ID. Replace the current shared staging plan through a separately approved migration."
}

locals {
  environment_name   = "production"
  customer_data_mode = "no-cui-only"
  data_posture       = "No-CUI / compliance management only"
  resource_group     = "gccs-production-rg"
  resource_id_prefix = "/subscriptions/${var.subscription_id}/resourceGroups/${local.resource_group}"
  common_tags = {
    Environment = "Production"
    DataPosture = "No-CUI"
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_resource_group" "production" {
  name     = local.resource_group
  location = "eastus"
  tags     = local.common_tags

  lifecycle { prevent_destroy = true }
}

resource "azurerm_virtual_network" "production" {
  name                = "gccs-production-vnet"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.production.name
  address_space       = ["10.0.0.0/16"]
  tags                = local.common_tags

  lifecycle { prevent_destroy = true }
}

resource "azurerm_subnet" "default" {
  name                 = "default"
  resource_group_name  = azurerm_resource_group.production.name
  virtual_network_name = azurerm_virtual_network.production.name
  address_prefixes     = ["10.0.0.0/24"]
}

resource "azurerm_subnet" "private_endpoints" {
  name                              = "default2"
  resource_group_name               = azurerm_resource_group.production.name
  virtual_network_name              = azurerm_virtual_network.production.name
  address_prefixes                  = ["10.0.2.0/24"]
  private_endpoint_network_policies = "Disabled"
}

resource "azurerm_subnet" "app_service" {
  name                 = "appservice-integration-subnet"
  resource_group_name  = azurerm_resource_group.production.name
  virtual_network_name = azurerm_virtual_network.production.name
  address_prefixes     = ["10.0.1.0/24"]

  delegation {
    name = "app-service"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_subnet" "malware_scanner" {
  name                 = "malware-scanner-subnet"
  resource_group_name  = azurerm_resource_group.production.name
  virtual_network_name = azurerm_virtual_network.production.name
  address_prefixes     = ["10.0.3.0/24"]

  delegation {
    name = "container-groups"
    service_delegation {
      name    = "Microsoft.ContainerInstance/containerGroups"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_postgresql_flexible_server" "production" {
  name                          = "gccs-postgres-production"
  resource_group_name           = azurerm_resource_group.production.name
  location                      = "eastus2"
  version                       = "17"
  zone                          = "1"
  sku_name                      = "B_Standard_B2s"
  storage_mb                    = 32768
  backup_retention_days         = 7
  geo_redundant_backup_enabled  = false
  public_network_access_enabled = true
  tags                          = local.common_tags

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [administrator_password]
  }
}

resource "azurerm_redis_cache" "production" {
  name                          = "gccs-redis-production"
  location                      = "eastus"
  resource_group_name           = azurerm_resource_group.production.name
  capacity                      = 1
  family                        = "C"
  sku_name                      = "Standard"
  redis_version                 = "6"
  minimum_tls_version           = "1.2"
  non_ssl_port_enabled          = false
  public_network_access_enabled = false
  tags                          = local.common_tags

  lifecycle { prevent_destroy = true }
}

resource "azurerm_storage_account" "production" {
  name                            = "gccsprodstore01"
  resource_group_name             = azurerm_resource_group.production.name
  location                        = "eastus"
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = false
  shared_access_key_enabled       = true
  tags                            = local.common_tags

  lifecycle { prevent_destroy = true }
}

resource "azurerm_private_dns_zone" "postgres" {
  name                = "privatelink.postgres.database.azure.com"
  resource_group_name = azurerm_resource_group.production.name
}

resource "azurerm_private_dns_zone" "redis" {
  name                = "privatelink.redis.cache.windows.net"
  resource_group_name = azurerm_resource_group.production.name
}

resource "azurerm_private_dns_zone" "blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = azurerm_resource_group.production.name
}

resource "azurerm_private_dns_zone_virtual_network_link" "postgres" {
  name                  = "gccs-production-vnet-postgres-dns-link"
  resource_group_name   = azurerm_resource_group.production.name
  private_dns_zone_name = azurerm_private_dns_zone.postgres.name
  virtual_network_id    = azurerm_virtual_network.production.id
}

resource "azurerm_private_dns_zone_virtual_network_link" "redis" {
  name                  = "gccs-production-vnet-redis-link"
  resource_group_name   = azurerm_resource_group.production.name
  private_dns_zone_name = azurerm_private_dns_zone.redis.name
  virtual_network_id    = azurerm_virtual_network.production.id
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  name                  = "gccs-production-vnet-blob-link"
  resource_group_name   = azurerm_resource_group.production.name
  private_dns_zone_name = azurerm_private_dns_zone.blob.name
  virtual_network_id    = azurerm_virtual_network.production.id
}

resource "azurerm_private_endpoint" "postgres" {
  name                = "gccs-postgres-production-pe"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.production.name
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "gccs-postgres-production-pe-connection"
    private_connection_resource_id = azurerm_postgresql_flexible_server.production.id
    subresource_names              = ["postgresqlServer"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [azurerm_private_dns_zone.postgres.id]
  }
}

resource "azurerm_private_endpoint" "redis" {
  name                = "gccs-redis-production-pe"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.production.name
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "gccs-redis-production-pe-connection"
    private_connection_resource_id = azurerm_redis_cache.production.id
    subresource_names              = ["redisCache"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [azurerm_private_dns_zone.redis.id]
  }
}

resource "azurerm_private_endpoint" "blob" {
  name                = "gccs-storage-production-blob-pe"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.production.name
  subnet_id           = azurerm_subnet.private_endpoints.id

  private_service_connection {
    name                           = "gccs-storage-production-blob-pe-connection"
    private_connection_resource_id = azurerm_storage_account.production.id
    subresource_names              = ["blob"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [azurerm_private_dns_zone.blob.id]
  }
}

resource "azurerm_linux_web_app" "api" {
  name                          = "gccs-api-production"
  location                      = "eastus"
  resource_group_name           = azurerm_resource_group.production.name
  service_plan_id               = var.production_service_plan_id
  https_only                    = true
  public_network_access_enabled = true
  virtual_network_subnet_id     = azurerm_subnet.app_service.id
  tags                          = local.common_tags

  identity { type = "SystemAssigned" }

  site_config {
    always_on           = true
    ftps_state          = "FtpsOnly"
    http2_enabled       = false
    minimum_tls_version = "1.2"
    websockets_enabled  = false
    application_stack { dotnet_version = "10.0" }
  }

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [app_settings]
  }
}

resource "azurerm_static_web_app" "web" {
  name                = "gccs-web-production"
  resource_group_name = azurerm_resource_group.production.name
  location            = "eastus2"
  sku_tier            = "Standard"
  sku_size            = "Standard"
  tags                = local.common_tags

  lifecycle { prevent_destroy = true }
}

resource "azurerm_container_group" "malware_scanner" {
  name                = "gccs-clamav-production"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.production.name
  ip_address_type     = "Private"
  subnet_ids          = [azurerm_subnet.malware_scanner.id]
  os_type             = "Linux"
  restart_policy      = "Always"
  sku                 = "Standard"
  tags                = local.common_tags

  container {
    name   = "gccs-clamav-production"
    image  = "clamav/clamav:stable"
    cpu    = 1
    memory = 2
    ports {
      port     = 3310
      protocol = "TCP"
    }
  }

  lifecycle { prevent_destroy = true }
}

resource "azurerm_application_insights" "api" {
  name                = "gccs-api-production"
  location            = "eastus"
  resource_group_name = azurerm_resource_group.production.name
  application_type    = "web"
  retention_in_days   = 90
  sampling_percentage = 0
  tags                = local.common_tags

  lifecycle { prevent_destroy = true }
}

resource "azurerm_monitor_action_group" "operations" {
  name                = "gccs-production-alert-ops"
  resource_group_name = azurerm_resource_group.production.name
  short_name          = "gccsprod"
  tags                = local.common_tags

  email_receiver {
    name                    = "launch-ops-email"
    email_address           = var.alert_email_address
    use_common_alert_schema = true
  }
}

resource "azurerm_monitor_metric_alert" "api_http_5xx" {
  name                = "gccs-api-production-http5xx"
  resource_group_name = azurerm_resource_group.production.name
  scopes              = [azurerm_linux_web_app.api.id]
  description         = "FeDril production API critical failure alert, including malware-scanner failures surfaced as HTTP 5xx."
  severity            = 2
  auto_mitigate       = false

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 0
  }

  action { action_group_id = azurerm_monitor_action_group.operations.id }
}

resource "azurerm_communication_service" "production" {
  name                = "gccs-acs-production"
  resource_group_name = azurerm_resource_group.production.name
  data_location       = "United States"
  tags                = local.common_tags
}

resource "azurerm_email_communication_service" "production" {
  name                = "gccs-email-production"
  resource_group_name = azurerm_resource_group.production.name
  data_location       = "United States"
  tags                = local.common_tags
}

resource "azurerm_email_communication_service_domain" "managed" {
  name              = "AzureManagedDomain"
  email_service_id  = azurerm_email_communication_service.production.id
  domain_management = "AzureManaged"
  tags              = local.common_tags
}

output "environment_contract" {
  value = {
    environment_name   = local.environment_name
    customer_data_mode = local.customer_data_mode
    data_posture       = local.data_posture
    resource_group_id  = azurerm_resource_group.production.id
    database           = azurerm_postgresql_flexible_server.production.id
    object_storage     = azurerm_storage_account.production.id
    cache              = azurerm_redis_cache.production.id
    queue              = azurerm_redis_cache.production.id
    secrets            = "App Service references and deployment-managed configuration; migrate to an approved key-management resource during hardening."
    background_jobs    = azurerm_linux_web_app.api.id
    health_checks      = "${azurerm_linux_web_app.api.default_hostname}/health"
    logs               = azurerm_application_insights.api.id
    alerts             = azurerm_monitor_metric_alert.api_http_5xx.id
  }
}
