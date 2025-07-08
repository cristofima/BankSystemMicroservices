using FluentAssertions;
using Security.Domain.Common;

namespace Security.Domain.UnitTests.Common;

public class AuditedEntityTests
{
    private class TestAuditedEntity : AuditedEntity
    {
    }

    [Fact]
    public void Constructor_ShouldSetCreatedAtToCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = new TestAuditedEntity();
        var afterCreation = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_ShouldInitializeUpdatedAtAsNull()
    {
        // Act
        var entity = new TestAuditedEntity();

        // Assert
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdatedAt_Setter_ShouldSetValue()
    {
        // Arrange
        var testDate = new DateTime(2023, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var entity = new TestAuditedEntity
        {
            // Act
            UpdatedAt = testDate
        };

        // Assert
        entity.UpdatedAt.Should().Be(testDate);
    }

    [Fact]
    public void Constructor_ShouldInitializeCreatedByAndUpdatedByAsNull()
    {
        // Act
        var entity = new TestAuditedEntity();

        // Assert
        entity.CreatedBy.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
    }
}
