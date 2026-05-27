using System.Collections.Generic;

namespace ManufacturingCostManagement.Web.Authorization
{
    /// Master list of permission codes and their default role mappings.
    public static class PermissionCatalog
    {
        public record PermissionItem(string Code, string Module, string Action, string Description);

        public static readonly string[] CrudActions = { "View", "Create", "Edit", "Delete" };

        public static readonly string[] CrudModules =
        {
            "Suppliers", "Materials", "Products", "Departments",
            "ProductionOrders", "BOM", "LaborCosts", "OverheadCosts"
        };

        public static readonly PermissionItem[] ExtraPermissions =
        {
            new("Reports.View", "Reports", "View", "View cost analysis and department reports"),
            new("Reports.LowStock", "Reports", "View", "View low-stock material report"),
            new("Dashboard.View", "Dashboard", "View", "View main dashboard"),
            new("Users.View",   "Users", "View",   "List users"),
            new("Users.Create", "Users", "Create", "Create new users"),
            new("Users.Edit",   "Users", "Edit",   "Edit users"),
            new("Users.Delete", "Users", "Delete", "Delete users"),
            new("Users.ResetPassword", "Users", "Manage", "Reset another user's password"),
            new("Permissions.Manage", "System", "Manage", "Configure role permissions")
        };

        public static IEnumerable<PermissionItem> All()
        {
            foreach (var module in CrudModules)
                foreach (var action in CrudActions)
                    yield return new PermissionItem(
                        $"{module}.{action}", module, action, $"{action} {module}");

            foreach (var p in ExtraPermissions) yield return p;
        }

        /// Default role -> permission code mapping.
        /// "*" means all permissions; otherwise list explicit codes.
        public static readonly Dictionary<string, string[]> DefaultRolePermissions = new()
        {
            ["Admin"] = new[] { "*" },
            ["Manager"] = new[]
            {
                "Dashboard.View", "Reports.View", "Reports.LowStock",
                "Suppliers.View", "Suppliers.Create", "Suppliers.Edit",
                "Materials.View", "Materials.Create", "Materials.Edit",
                "Products.View", "Products.Create", "Products.Edit",
                "Departments.View", "Departments.Create", "Departments.Edit",
                "ProductionOrders.View", "ProductionOrders.Create", "ProductionOrders.Edit",
                "BOM.View", "BOM.Create", "BOM.Edit", "BOM.Delete",
                "LaborCosts.View", "LaborCosts.Create", "LaborCosts.Edit",
                "OverheadCosts.View", "OverheadCosts.Create", "OverheadCosts.Edit"
            },
            ["Accountant"] = new[]
            {
                "Dashboard.View", "Reports.View", "Reports.LowStock",
                "Suppliers.View",
                "Materials.View",
                "Products.View",
                "Departments.View",
                "ProductionOrders.View",
                "BOM.View",
                "LaborCosts.View", "LaborCosts.Create", "LaborCosts.Edit",
                "OverheadCosts.View", "OverheadCosts.Create", "OverheadCosts.Edit"
            },
            ["Employee"] = new[]
            {
                "Dashboard.View",
                "Suppliers.View", "Materials.View", "Products.View",
                "Departments.View", "ProductionOrders.View",
                "BOM.View", "LaborCosts.View", "OverheadCosts.View"
            }
        };
    }
}
