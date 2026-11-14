using System;
using MySql.Data.MySqlClient;
using HeatTransferSim.Models;

namespace HeatTransferSim.Services;

public static class DatabaseService
{
    // Ajuste a string de conexão conforme o seu ambiente MySQL local
    private const string ConnectionString = "Server=localhost;Database=heat_transfer_db;Uid=root;Pwd=;";

    public static void InitializeDatabase()
    {
        try
        {
            // Primeiro conecta sem o banco para garantir que ele existe
            var builder = new MySqlConnectionStringBuilder(ConnectionString);
            string dbName = builder.Database;
            builder.Database = "";

            using (var conn = new MySqlConnection(builder.ConnectionString))
            {
                conn.Open();
                using var cmd = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{dbName}`;", conn);
                cmd.ExecuteNonQuery();
            }

            // Agora cria a tabela
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    CREATE TABLE IF NOT EXISTS simulations (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        material_name VARCHAR(100),
                        thermal_conductivity DOUBLE,
                        specific_heat DOUBLE,
                        density DOUBLE,
                        cube_side DOUBLE,
                        grid_size INT,
                        simulation_time DOUBLE,
                        min_temperature DOUBLE,
                        max_temperature DOUBLE,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";
                using var cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show($"Erro ao inicializar o banco de dados MySQL: {ex.Message}", "Erro de Banco de Dados", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    public static void SaveSimulation(HeatGrid grid)
    {
        try
        {
            if (grid == null) return;

            var (minTemp, maxTemp) = grid.GetTemperatureRange();
            var material = grid.Bodies[0, 0].Material;

            using var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            string query = @"
                INSERT INTO simulations 
                (material_name, thermal_conductivity, specific_heat, density, cube_side, grid_size, simulation_time, min_temperature, max_temperature) 
                VALUES 
                (@matName, @cond, @specHeat, @density, @side, @size, @simTime, @minTemp, @maxTemp)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@matName", material.Name);
            cmd.Parameters.AddWithValue("@cond", material.ThermalConductivity);
            cmd.Parameters.AddWithValue("@specHeat", material.SpecificHeat);
            cmd.Parameters.AddWithValue("@density", material.Density);
            cmd.Parameters.AddWithValue("@side", grid.Side);
            cmd.Parameters.AddWithValue("@size", grid.Size);
            cmd.Parameters.AddWithValue("@simTime", grid.Time);
            cmd.Parameters.AddWithValue("@minTemp", minTemp);
            cmd.Parameters.AddWithValue("@maxTemp", maxTemp);

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show($"Erro ao salvar simulação no MySQL: {ex.Message}", "Erro de Banco de Dados", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }
    }
}
