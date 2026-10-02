using TreeEditor.Domain.Entities;

namespace TreeEditor.Infrastructure.Persistence.Seed;

public static class InitialTreeData
{
    public static IReadOnlyCollection<TreeNode> Create()
    {
        var companyId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        var engineeringId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var backendId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var platformId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var apiId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var workersId = Guid.Parse("50000000-0000-0000-0000-000000000002");

        var integrationsId = Guid.Parse("40000000-0000-0000-0000-000000000002");

        var frontendId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var webId = Guid.Parse("40000000-0000-0000-0000-000000000003");
        var componentsId = Guid.Parse("50000000-0000-0000-0000-000000000003");

        var salesId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var europeId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var ukraineId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        var kyivId = Guid.Parse("50000000-0000-0000-0000-000000000004");
        var lvivId = Guid.Parse("50000000-0000-0000-0000-000000000005");

        var usaId = Guid.Parse("30000000-0000-0000-0000-000000000004");

        return
        [
            new TreeNode(companyId, null, "Company"),

            new TreeNode(engineeringId, companyId, "Engineering"),
            new TreeNode(backendId, engineeringId, "Backend"),
            new TreeNode(platformId, backendId, "Platform"),
            new TreeNode(apiId, platformId, "API"),
            new TreeNode(workersId, platformId, "Workers"),
            new TreeNode(integrationsId, backendId, "Integrations"),

            new TreeNode(frontendId, engineeringId, "Frontend"),
            new TreeNode(webId, frontendId, "Web"),
            new TreeNode(componentsId, webId, "Components"),

            new TreeNode(salesId, companyId, "Sales"),
            new TreeNode(europeId, salesId, "Europe"),
            new TreeNode(ukraineId, europeId, "Ukraine"),
            new TreeNode(kyivId, ukraineId, "Kyiv"),
            new TreeNode(lvivId, ukraineId, "Lviv"),

            new TreeNode(usaId, salesId, "USA")
        ];
    }
}