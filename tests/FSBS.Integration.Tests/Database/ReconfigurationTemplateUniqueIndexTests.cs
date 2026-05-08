using FSBS.Integration.Tests.Infrastructure;
using Npgsql;

namespace FSBS.Integration.Tests.Database;

/// <summary>
/// Asserts <c>uq_reconfig_templates_pair</c> on
/// <c>reconfiguration_templates(from_config_id, to_config_id)</c> and the
/// related <c>ck_reconfig_templates_different</c> CHECK that bans self-pairs.
/// </summary>
[Trait("Category", "Integration")]
public class ReconfigurationTemplateUniqueIndexTests(PostgresFixture postgres) : DatabaseTestBase(postgres)
{
    [Fact]
    public async Task DuplicatePair_RaisesUniqueViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var fromConfig = await DbSeeder.SeedSimulatorConfigurationAsync(conn);
        var toConfig = await DbSeeder.SeedSimulatorConfigurationAsync(conn);

        await InsertTemplateAsync(conn, fromConfig, toConfig, durationMins: 60);

        var act = () => InsertTemplateAsync(conn, fromConfig, toConfig, durationMins: 90);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.UniqueViolation);
        ex.Which.ConstraintName.Should().Be("uq_reconfig_templates_pair");
    }

    [Fact]
    public async Task ReversePair_IsTreatedAsDistinct()
    {
        // (A→B) and (B→A) are different pairs — both rows are legal.
        await using var conn = await OpenConnectionAsync();
        var configA = await DbSeeder.SeedSimulatorConfigurationAsync(conn);
        var configB = await DbSeeder.SeedSimulatorConfigurationAsync(conn);

        await InsertTemplateAsync(conn, configA, configB, durationMins: 60);
        await InsertTemplateAsync(conn, configB, configA, durationMins: 90);
    }

    [Fact]
    public async Task SameConfigOnBothSides_RaisesCheckViolation()
    {
        await using var conn = await OpenConnectionAsync();
        var configId = await DbSeeder.SeedSimulatorConfigurationAsync(conn);

        var act = () => InsertTemplateAsync(conn, configId, configId, durationMins: 60);

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be(PostgresExceptionAssertions.CheckViolation);
        ex.Which.ConstraintName.Should().Be("ck_reconfig_templates_different");
    }

    private static async Task InsertTemplateAsync(
        NpgsqlConnection conn, Guid fromConfigId, Guid toConfigId, int durationMins)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO reconfiguration_templates
                (reconfig_template_id, from_config_id, to_config_id, duration_mins)
            VALUES (@id, @from, @to, @d);
            """, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("from", fromConfigId);
        cmd.Parameters.AddWithValue("to", toConfigId);
        cmd.Parameters.AddWithValue("d", durationMins);
        await cmd.ExecuteNonQueryAsync();
    }
}
