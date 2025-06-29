using Npgsql;
using Respawn;

namespace Chronofoil.Web.IntegrationTests;

public class RespawnWrapper
{
    private Respawner _respawner;
    private readonly string _connectionString;
    
    private RespawnWrapper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<RespawnWrapper> CreateAsync(string connectionString)
    {
        var wrapper = new RespawnWrapper(connectionString);
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        wrapper._respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });
        return wrapper;
    }

    public async Task ResetAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}