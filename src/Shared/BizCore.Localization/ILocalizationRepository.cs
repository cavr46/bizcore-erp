namespace BizCore.Localization;

/// <summary>
/// Localization repository interface
/// </summary>
public interface ILocalizationRepository
{
    Task<Dictionary<string, string>> GetTranslationsAsync(string cultureName);
    Task<string> GetTranslationAsync(string key, string cultureName);
    Task AddOrUpdateTranslationAsync(string key, string cultureName, string value);
    Task RemoveTranslationAsync(string key, string cultureName);
    Task<IEnumerable<string>> GetAllKeysAsync(string cultureName);
    Task<IEnumerable<string>> GetSupportedCulturesAsync();
    Task<bool> ExistsAsync(string key, string cultureName);
}

/// <summary>
/// SQL Server localization repository
/// </summary>
public class SqlLocalizationRepository : ILocalizationRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlLocalizationRepository> _logger;

    public SqlLocalizationRepository(IConfiguration configuration, ILogger<SqlLocalizationRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string cultureName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT [Key], [Value] 
                FROM [Localization] 
                WHERE [Culture] = @Culture AND [IsActive] = 1
                ORDER BY [Key]";

            var translations = await connection.QueryAsync<(string Key, string Value)>(query, new { Culture = cultureName });
            
            return translations.ToDictionary(t => t.Key, t => t.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get translations for culture {Culture}", cultureName);
            return new Dictionary<string, string>();
        }
    }

    public async Task<string> GetTranslationAsync(string key, string cultureName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT [Value] 
                FROM [Localization] 
                WHERE [Key] = @Key AND [Culture] = @Culture AND [IsActive] = 1";

            return await connection.QueryFirstOrDefaultAsync<string>(query, new { Key = key, Culture = cultureName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get translation for key {Key} in culture {Culture}", key, cultureName);
            return null;
        }
    }

    public async Task AddOrUpdateTranslationAsync(string key, string cultureName, string value)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                MERGE [Localization] AS target
                USING (VALUES (@Key, @Culture, @Value)) AS source ([Key], [Culture], [Value])
                ON target.[Key] = source.[Key] AND target.[Culture] = source.[Culture]
                WHEN MATCHED THEN
                    UPDATE SET [Value] = source.[Value], [UpdatedAt] = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT ([Key], [Culture], [Value], [CreatedAt], [UpdatedAt], [IsActive])
                    VALUES (source.[Key], source.[Culture], source.[Value], GETUTCDATE(), GETUTCDATE(), 1);";

            await connection.ExecuteAsync(query, new { Key = key, Culture = cultureName, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update translation for key {Key} in culture {Culture}", key, cultureName);
            throw;
        }
    }

    public async Task RemoveTranslationAsync(string key, string cultureName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE [Localization] 
                SET [IsActive] = 0, [UpdatedAt] = GETUTCDATE()
                WHERE [Key] = @Key AND [Culture] = @Culture";

            await connection.ExecuteAsync(query, new { Key = key, Culture = cultureName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove translation for key {Key} in culture {Culture}", key, cultureName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAllKeysAsync(string cultureName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT [Key] 
                FROM [Localization] 
                WHERE [Culture] = @Culture AND [IsActive] = 1
                ORDER BY [Key]";

            return await connection.QueryAsync<string>(query, new { Culture = cultureName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all keys for culture {Culture}", cultureName);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetSupportedCulturesAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT [Culture] 
                FROM [Localization] 
                WHERE [IsActive] = 1
                ORDER BY [Culture]";

            return await connection.QueryAsync<string>(query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get supported cultures");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> ExistsAsync(string key, string cultureName)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(1) 
                FROM [Localization] 
                WHERE [Key] = @Key AND [Culture] = @Culture AND [IsActive] = 1";

            var count = await connection.QueryFirstAsync<int>(query, new { Key = key, Culture = cultureName });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if translation exists for key {Key} in culture {Culture}", key, cultureName);
            return false;
        }
    }
}

/// <summary>
/// JSON file localization repository (for development/testing)
/// </summary>
public class JsonLocalizationRepository : ILocalizationRepository
{
    private readonly string _basePath;
    private readonly ILogger<JsonLocalizationRepository> _logger;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();

    public JsonLocalizationRepository(IWebHostEnvironment environment, ILogger<JsonLocalizationRepository> logger)
    {
        _basePath = Path.Combine(environment.ContentRootPath, "Localization");
        _logger = logger;
        
        // Ensure directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string cultureName)
    {
        try
        {
            if (_cache.TryGetValue(cultureName, out var cachedTranslations))
            {
                return cachedTranslations;
            }

            var filePath = Path.Combine(_basePath, $"{cultureName}.json");
            
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, string>();
            }

            var json = await File.ReadAllTextAsync(filePath);
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            
            _cache.TryAdd(cultureName, translations);
            return translations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get translations for culture {Culture}", cultureName);
            return new Dictionary<string, string>();
        }
    }

    public async Task<string> GetTranslationAsync(string key, string cultureName)
    {
        var translations = await GetTranslationsAsync(cultureName);
        return translations.GetValueOrDefault(key);
    }

    public async Task AddOrUpdateTranslationAsync(string key, string cultureName, string value)
    {
        try
        {
            var translations = await GetTranslationsAsync(cultureName);
            translations[key] = value;

            var filePath = Path.Combine(_basePath, $"{cultureName}.json");
            var json = JsonSerializer.Serialize(translations, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            // Update cache
            _cache.AddOrUpdate(cultureName, translations, (k, v) => translations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update translation for key {Key} in culture {Culture}", key, cultureName);
            throw;
        }
    }

    public async Task RemoveTranslationAsync(string key, string cultureName)
    {
        try
        {
            var translations = await GetTranslationsAsync(cultureName);
            if (translations.Remove(key))
            {
                var filePath = Path.Combine(_basePath, $"{cultureName}.json");
                var json = JsonSerializer.Serialize(translations, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);

                // Update cache
                _cache.AddOrUpdate(cultureName, translations, (k, v) => translations);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove translation for key {Key} in culture {Culture}", key, cultureName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAllKeysAsync(string cultureName)
    {
        var translations = await GetTranslationsAsync(cultureName);
        return translations.Keys;
    }

    public async Task<IEnumerable<string>> GetSupportedCulturesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_basePath, "*.json");
            return files.Select(f => Path.GetFileNameWithoutExtension(f));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get supported cultures");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> ExistsAsync(string key, string cultureName)
    {
        var translations = await GetTranslationsAsync(cultureName);
        return translations.ContainsKey(key);
    }
}

/// <summary>
/// Database migration for localization
/// </summary>
public static class LocalizationDatabaseMigration
{
    public static async Task EnsureTablesExistAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var createTableQuery = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Localization')
            BEGIN
                CREATE TABLE [Localization] (
                    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                    [Key] NVARCHAR(500) NOT NULL,
                    [Culture] NVARCHAR(10) NOT NULL,
                    [Value] NVARCHAR(MAX) NOT NULL,
                    [Category] NVARCHAR(100) NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [IsActive] BIT NOT NULL DEFAULT 1
                );

                CREATE UNIQUE INDEX [IX_Localization_Key_Culture] ON [Localization] ([Key], [Culture]);
                CREATE INDEX [IX_Localization_Culture] ON [Localization] ([Culture]);
                CREATE INDEX [IX_Localization_Category] ON [Localization] ([Category]);
                CREATE INDEX [IX_Localization_IsActive] ON [Localization] ([IsActive]);
            END";

        await connection.ExecuteAsync(createTableQuery);
    }

    public static async Task SeedDefaultTranslationsAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var seedData = GetDefaultTranslations();
        
        foreach (var (culture, translations) in seedData)
        {
            foreach (var (key, value) in translations)
            {
                var query = @"
                    IF NOT EXISTS (SELECT 1 FROM [Localization] WHERE [Key] = @Key AND [Culture] = @Culture)
                    BEGIN
                        INSERT INTO [Localization] ([Key], [Culture], [Value], [Category], [CreatedAt], [UpdatedAt], [IsActive])
                        VALUES (@Key, @Culture, @Value, @Category, GETUTCDATE(), GETUTCDATE(), 1)
                    END";

                await connection.ExecuteAsync(query, new 
                { 
                    Key = key, 
                    Culture = culture, 
                    Value = value,
                    Category = GetCategoryFromKey(key)
                });
            }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> GetDefaultTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string>
            {
                // Common
                ["Common.Save"] = "Save",
                ["Common.Cancel"] = "Cancel",
                ["Common.Delete"] = "Delete",
                ["Common.Edit"] = "Edit",
                ["Common.Create"] = "Create",
                ["Common.Update"] = "Update",
                ["Common.Search"] = "Search",
                ["Common.Filter"] = "Filter",
                ["Common.Export"] = "Export",
                ["Common.Import"] = "Import",
                ["Common.Print"] = "Print",
                ["Common.Close"] = "Close",
                ["Common.Yes"] = "Yes",
                ["Common.No"] = "No",
                ["Common.OK"] = "OK",
                ["Common.Back"] = "Back",
                ["Common.Next"] = "Next",
                ["Common.Previous"] = "Previous",
                ["Common.Loading"] = "Loading...",
                ["Common.Error"] = "Error",
                ["Common.Warning"] = "Warning",
                ["Common.Success"] = "Success",
                ["Common.Information"] = "Information",
                
                // Navigation
                ["Navigation.Dashboard"] = "Dashboard",
                ["Navigation.Accounting"] = "Accounting",
                ["Navigation.Inventory"] = "Inventory",
                ["Navigation.Sales"] = "Sales",
                ["Navigation.Purchasing"] = "Purchasing",
                ["Navigation.HumanResources"] = "Human Resources",
                ["Navigation.Manufacturing"] = "Manufacturing",
                ["Navigation.Reports"] = "Reports",
                ["Navigation.Settings"] = "Settings",
                ["Navigation.Help"] = "Help",
                ["Navigation.Logout"] = "Logout",
                
                // Dashboard
                ["Dashboard.Title"] = "Dashboard",
                ["Dashboard.WelcomeMessage"] = "Welcome to BizCore ERP - Your modern business management solution",
                ["Dashboard.MonthlyRevenue"] = "Monthly Revenue",
                ["Dashboard.TotalOrders"] = "Total Orders",
                ["Dashboard.ActiveCustomers"] = "Active Customers",
                ["Dashboard.LowStockItems"] = "Low Stock Items",
                ["Dashboard.RecentOrders"] = "Recent Orders",
                ["Dashboard.QuickActions"] = "Quick Actions",
                ["Dashboard.CreateSalesOrder"] = "Create Sales Order",
                ["Dashboard.CreatePurchaseOrder"] = "Create Purchase Order",
                ["Dashboard.CreateJournalEntry"] = "Create Journal Entry",
                ["Dashboard.AddProduct"] = "Add Product",
                
                // Accounting
                ["Accounting.ChartOfAccounts"] = "Chart of Accounts",
                ["Accounting.JournalEntries"] = "Journal Entries",
                ["Accounting.GeneralLedger"] = "General Ledger",
                ["Accounting.TrialBalance"] = "Trial Balance",
                ["Accounting.FinancialStatements"] = "Financial Statements",
                ["Accounting.Account"] = "Account",
                ["Accounting.AccountName"] = "Account Name",
                ["Accounting.AccountType"] = "Account Type",
                ["Accounting.Balance"] = "Balance",
                ["Accounting.Debit"] = "Debit",
                ["Accounting.Credit"] = "Credit",
                
                // Inventory
                ["Inventory.Products"] = "Products",
                ["Inventory.StockLevels"] = "Stock Levels",
                ["Inventory.Warehouses"] = "Warehouses",
                ["Inventory.StockMovements"] = "Stock Movements",
                ["Inventory.Product"] = "Product",
                ["Inventory.ProductName"] = "Product Name",
                ["Inventory.SKU"] = "SKU",
                ["Inventory.Quantity"] = "Quantity",
                ["Inventory.UnitPrice"] = "Unit Price",
                ["Inventory.TotalValue"] = "Total Value",
                
                // Sales
                ["Sales.Customers"] = "Customers",
                ["Sales.Quotations"] = "Quotations",
                ["Sales.SalesOrders"] = "Sales Orders",
                ["Sales.Invoices"] = "Invoices",
                ["Sales.Customer"] = "Customer",
                ["Sales.CustomerName"] = "Customer Name",
                ["Sales.OrderNumber"] = "Order Number",
                ["Sales.OrderDate"] = "Order Date",
                ["Sales.Amount"] = "Amount",
                ["Sales.Status"] = "Status",
                
                // Purchasing
                ["Purchasing.Suppliers"] = "Suppliers",
                ["Purchasing.PurchaseOrders"] = "Purchase Orders",
                ["Purchasing.GoodsReceipts"] = "Goods Receipts",
                ["Purchasing.Bills"] = "Bills",
                ["Purchasing.Supplier"] = "Supplier",
                ["Purchasing.SupplierName"] = "Supplier Name",
                
                // Human Resources
                ["HR.Employees"] = "Employees",
                ["HR.Attendance"] = "Attendance",
                ["HR.Payroll"] = "Payroll",
                ["HR.LeaveManagement"] = "Leave Management",
                ["HR.Employee"] = "Employee",
                ["HR.EmployeeName"] = "Employee Name",
                ["HR.Department"] = "Department",
                ["HR.Position"] = "Position",
                
                // Manufacturing
                ["Manufacturing.BillOfMaterials"] = "Bill of Materials",
                ["Manufacturing.WorkOrders"] = "Work Orders",
                ["Manufacturing.Production"] = "Production",
                ["Manufacturing.QualityControl"] = "Quality Control",
                
                // Validation Messages
                ["Validation.Required"] = "This field is required",
                ["Validation.Email"] = "Please enter a valid email address",
                ["Validation.Phone"] = "Please enter a valid phone number",
                ["Validation.MinLength"] = "Minimum length is {0} characters",
                ["Validation.MaxLength"] = "Maximum length is {0} characters",
                ["Validation.Range"] = "Value must be between {0} and {1}",
                
                // Error Messages
                ["Error.General"] = "An error occurred. Please try again.",
                ["Error.NotFound"] = "The requested item was not found.",
                ["Error.Unauthorized"] = "You are not authorized to perform this action.",
                ["Error.ValidationFailed"] = "Validation failed. Please check your input.",
                ["Error.NetworkError"] = "Network error. Please check your connection.",
                
                // Success Messages
                ["Success.Saved"] = "Data saved successfully",
                ["Success.Updated"] = "Data updated successfully",
                ["Success.Deleted"] = "Data deleted successfully",
                ["Success.Created"] = "Data created successfully",
                ["Success.Imported"] = "Data imported successfully",
                ["Success.Exported"] = "Data exported successfully"
            },
            
            ["es-ES"] = new Dictionary<string, string>
            {
                // Common
                ["Common.Save"] = "Guardar",
                ["Common.Cancel"] = "Cancelar",
                ["Common.Delete"] = "Eliminar",
                ["Common.Edit"] = "Editar",
                ["Common.Create"] = "Crear",
                ["Common.Update"] = "Actualizar",
                ["Common.Search"] = "Buscar",
                ["Common.Filter"] = "Filtrar",
                ["Common.Export"] = "Exportar",
                ["Common.Import"] = "Importar",
                ["Common.Print"] = "Imprimir",
                ["Common.Close"] = "Cerrar",
                ["Common.Yes"] = "Sí",
                ["Common.No"] = "No",
                ["Common.OK"] = "Aceptar",
                ["Common.Back"] = "Atrás",
                ["Common.Next"] = "Siguiente",
                ["Common.Previous"] = "Anterior",
                ["Common.Loading"] = "Cargando...",
                ["Common.Error"] = "Error",
                ["Common.Warning"] = "Advertencia",
                ["Common.Success"] = "Éxito",
                ["Common.Information"] = "Información",
                
                // Navigation
                ["Navigation.Dashboard"] = "Panel de Control",
                ["Navigation.Accounting"] = "Contabilidad",
                ["Navigation.Inventory"] = "Inventario",
                ["Navigation.Sales"] = "Ventas",
                ["Navigation.Purchasing"] = "Compras",
                ["Navigation.HumanResources"] = "Recursos Humanos",
                ["Navigation.Manufacturing"] = "Fabricación",
                ["Navigation.Reports"] = "Reportes",
                ["Navigation.Settings"] = "Configuración",
                ["Navigation.Help"] = "Ayuda",
                ["Navigation.Logout"] = "Cerrar Sesión",
                
                // Dashboard
                ["Dashboard.Title"] = "Panel de Control",
                ["Dashboard.WelcomeMessage"] = "Bienvenido a BizCore ERP - Tu solución moderna de gestión empresarial",
                ["Dashboard.MonthlyRevenue"] = "Ingresos Mensuales",
                ["Dashboard.TotalOrders"] = "Pedidos Totales",
                ["Dashboard.ActiveCustomers"] = "Clientes Activos",
                ["Dashboard.LowStockItems"] = "Productos con Stock Bajo",
                ["Dashboard.RecentOrders"] = "Pedidos Recientes",
                ["Dashboard.QuickActions"] = "Acciones Rápidas",
                ["Dashboard.CreateSalesOrder"] = "Crear Pedido de Venta",
                ["Dashboard.CreatePurchaseOrder"] = "Crear Orden de Compra",
                ["Dashboard.CreateJournalEntry"] = "Crear Asiento Contable",
                ["Dashboard.AddProduct"] = "Agregar Producto",
                
                // Accounting
                ["Accounting.ChartOfAccounts"] = "Plan de Cuentas",
                ["Accounting.JournalEntries"] = "Asientos Contables",
                ["Accounting.GeneralLedger"] = "Libro Mayor",
                ["Accounting.TrialBalance"] = "Balance de Comprobación",
                ["Accounting.FinancialStatements"] = "Estados Financieros",
                ["Accounting.Account"] = "Cuenta",
                ["Accounting.AccountName"] = "Nombre de Cuenta",
                ["Accounting.AccountType"] = "Tipo de Cuenta",
                ["Accounting.Balance"] = "Saldo",
                ["Accounting.Debit"] = "Débito",
                ["Accounting.Credit"] = "Crédito",
                
                // Inventory
                ["Inventory.Products"] = "Productos",
                ["Inventory.StockLevels"] = "Niveles de Stock",
                ["Inventory.Warehouses"] = "Almacenes",
                ["Inventory.StockMovements"] = "Movimientos de Stock",
                ["Inventory.Product"] = "Producto",
                ["Inventory.ProductName"] = "Nombre del Producto",
                ["Inventory.SKU"] = "Código",
                ["Inventory.Quantity"] = "Cantidad",
                ["Inventory.UnitPrice"] = "Precio Unitario",
                ["Inventory.TotalValue"] = "Valor Total",
                
                // Sales
                ["Sales.Customers"] = "Clientes",
                ["Sales.Quotations"] = "Cotizaciones",
                ["Sales.SalesOrders"] = "Pedidos de Venta",
                ["Sales.Invoices"] = "Facturas",
                ["Sales.Customer"] = "Cliente",
                ["Sales.CustomerName"] = "Nombre del Cliente",
                ["Sales.OrderNumber"] = "Número de Pedido",
                ["Sales.OrderDate"] = "Fecha del Pedido",
                ["Sales.Amount"] = "Importe",
                ["Sales.Status"] = "Estado",
                
                // Purchasing
                ["Purchasing.Suppliers"] = "Proveedores",
                ["Purchasing.PurchaseOrders"] = "Órdenes de Compra",
                ["Purchasing.GoodsReceipts"] = "Recepción de Mercancía",
                ["Purchasing.Bills"] = "Facturas de Compra",
                ["Purchasing.Supplier"] = "Proveedor",
                ["Purchasing.SupplierName"] = "Nombre del Proveedor",
                
                // Human Resources
                ["HR.Employees"] = "Empleados",
                ["HR.Attendance"] = "Asistencia",
                ["HR.Payroll"] = "Nómina",
                ["HR.LeaveManagement"] = "Gestión de Ausencias",
                ["HR.Employee"] = "Empleado",
                ["HR.EmployeeName"] = "Nombre del Empleado",
                ["HR.Department"] = "Departamento",
                ["HR.Position"] = "Puesto",
                
                // Manufacturing
                ["Manufacturing.BillOfMaterials"] = "Lista de Materiales",
                ["Manufacturing.WorkOrders"] = "Órdenes de Trabajo",
                ["Manufacturing.Production"] = "Producción",
                ["Manufacturing.QualityControl"] = "Control de Calidad",
                
                // Validation Messages
                ["Validation.Required"] = "Este campo es obligatorio",
                ["Validation.Email"] = "Por favor ingrese una dirección de email válida",
                ["Validation.Phone"] = "Por favor ingrese un número de teléfono válido",
                ["Validation.MinLength"] = "La longitud mínima es {0} caracteres",
                ["Validation.MaxLength"] = "La longitud máxima es {0} caracteres",
                ["Validation.Range"] = "El valor debe estar entre {0} y {1}",
                
                // Error Messages
                ["Error.General"] = "Ocurrió un error. Por favor intente nuevamente.",
                ["Error.NotFound"] = "El elemento solicitado no fue encontrado.",
                ["Error.Unauthorized"] = "No está autorizado para realizar esta acción.",
                ["Error.ValidationFailed"] = "La validación falló. Por favor verifique su entrada.",
                ["Error.NetworkError"] = "Error de red. Por favor verifique su conexión.",
                
                // Success Messages
                ["Success.Saved"] = "Datos guardados exitosamente",
                ["Success.Updated"] = "Datos actualizados exitosamente",
                ["Success.Deleted"] = "Datos eliminados exitosamente",
                ["Success.Created"] = "Datos creados exitosamente",
                ["Success.Imported"] = "Datos importados exitosamente",
                ["Success.Exported"] = "Datos exportados exitosamente"
            }
        };
    }

    private static string GetCategoryFromKey(string key)
    {
        var parts = key.Split('.');
        return parts.Length > 1 ? parts[0] : "General";
    }
}