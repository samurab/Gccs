# Import only after the remote backend is created, state access is restricted,
# and the generated plan has been reviewed. Import does not change Azure.
import {
  to = azurerm_resource_group.production
  id = local.resource_id_prefix
}
import {
  to = azurerm_virtual_network.production
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/virtualNetworks/gccs-production-vnet"
}
import {
  to = azurerm_subnet.default
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/virtualNetworks/gccs-production-vnet/subnets/default"
}
import {
  to = azurerm_subnet.private_endpoints
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/virtualNetworks/gccs-production-vnet/subnets/default2"
}
import {
  to = azurerm_subnet.app_service
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/virtualNetworks/gccs-production-vnet/subnets/appservice-integration-subnet"
}
import {
  to = azurerm_subnet.malware_scanner
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/virtualNetworks/gccs-production-vnet/subnets/malware-scanner-subnet"
}
import {
  to = azurerm_postgresql_flexible_server.production
  id = "${local.resource_id_prefix}/providers/Microsoft.DBforPostgreSQL/flexibleServers/gccs-postgres-production"
}
import {
  to = azurerm_redis_cache.production
  id = "${local.resource_id_prefix}/providers/Microsoft.Cache/Redis/gccs-redis-production"
}
import {
  to = azurerm_storage_account.production
  id = "${local.resource_id_prefix}/providers/Microsoft.Storage/storageAccounts/gccsprodstore01"
}
import {
  to = azurerm_private_dns_zone.postgres
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateDnsZones/privatelink.postgres.database.azure.com"
}
import {
  to = azurerm_private_dns_zone.redis
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateDnsZones/privatelink.redis.cache.windows.net"
}
import {
  to = azurerm_private_dns_zone.blob
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateDnsZones/privatelink.blob.core.windows.net"
}
import {
  to = azurerm_private_dns_zone_virtual_network_link.postgres
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateDnsZones/privatelink.postgres.database.azure.com/virtualNetworkLinks/gccs-production-vnet-postgres-dns-link"
}
import {
  to = azurerm_private_dns_zone_virtual_network_link.redis
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateDnsZones/privatelink.redis.cache.windows.net/virtualNetworkLinks/gccs-production-vnet-redis-link"
}
import {
  to = azurerm_private_dns_zone_virtual_network_link.blob
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateDnsZones/privatelink.blob.core.windows.net/virtualNetworkLinks/gccs-production-vnet-blob-link"
}
import {
  to = azurerm_private_endpoint.postgres
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateEndpoints/gccs-postgres-production-pe"
}
import {
  to = azurerm_private_endpoint.redis
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateEndpoints/gccs-redis-production-pe"
}
import {
  to = azurerm_private_endpoint.blob
  id = "${local.resource_id_prefix}/providers/Microsoft.Network/privateEndpoints/gccs-storage-production-blob-pe"
}
import {
  to = azurerm_linux_web_app.api
  id = "${local.resource_id_prefix}/providers/Microsoft.Web/sites/gccs-api-production"
}
import {
  to = azurerm_static_web_app.web
  id = "${local.resource_id_prefix}/providers/Microsoft.Web/staticSites/gccs-web-production"
}
import {
  to = azurerm_container_group.malware_scanner
  id = "${local.resource_id_prefix}/providers/Microsoft.ContainerInstance/containerGroups/gccs-clamav-production"
}
import {
  to = azurerm_application_insights.api
  id = "${local.resource_id_prefix}/providers/Microsoft.Insights/components/gccs-api-production"
}
import {
  to = azurerm_monitor_action_group.operations
  id = "${local.resource_id_prefix}/providers/Microsoft.Insights/actionGroups/gccs-production-alert-ops"
}
import {
  to = azurerm_monitor_metric_alert.api_http_5xx
  id = "${local.resource_id_prefix}/providers/Microsoft.Insights/metricAlerts/gccs-api-production-http5xx"
}
import {
  to = azurerm_communication_service.production
  id = "${local.resource_id_prefix}/providers/Microsoft.Communication/CommunicationServices/gccs-acs-production"
}
import {
  to = azurerm_email_communication_service.production
  id = "${local.resource_id_prefix}/providers/Microsoft.Communication/EmailServices/gccs-email-production"
}
import {
  to = azurerm_email_communication_service_domain.managed
  id = "${local.resource_id_prefix}/providers/Microsoft.Communication/EmailServices/gccs-email-production/Domains/AzureManagedDomain"
}
