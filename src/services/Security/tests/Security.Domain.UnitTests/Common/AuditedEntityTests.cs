using FluentAssertions;
using BankSystem.Shared.Domain.Common;

namespace Security.Domain.UnitTests.Common;

public class AuditedEntityTests
{
    private class TestAuditedEntity : AuditedEntity
    {
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesAsNull()
    {
        // Act
        var entity = new TestAuditedEntity();

        // Assert
        entity.UpdatedAt.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
    }
}
