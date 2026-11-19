using System.Data;
using Microsoft.Data.SqlClient;

namespace lab08.Data;


// ADO.NET, приєднана модель: SqlConnection + SqlCommand + SqlDataReader.
internal sealed class DiscRepository {
    private readonly string _connectionString;

    public string Server => @"(localdb)\MSSQLLocalDB";
    public string Database => "PTNET_Lab08";

    public DiscRepository() {
        var builder = new SqlConnectionStringBuilder {
            DataSource = Server,
            InitialCatalog = Database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        };
        builder["Encrypt"] = "True";
        _connectionString = builder.ConnectionString;
    }

    public void Initialize() {
        var masterBuilder = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master" };
        using (var master = new SqlConnection(masterBuilder.ConnectionString)) {
            master.Open();

            using var check = new SqlCommand("SELECT DB_ID(@name)", master);
            check.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = Database;
            if (check.ExecuteScalar() is DBNull) {
                var quotedName = "[" + Database.Replace("]", "]]") + "]";
                using var create = new SqlCommand($"CREATE DATABASE {quotedName}", master);
                create.ExecuteNonQuery();
            }
        }

        using var connection = Open();
        bool tableWasMissing;
        using (var checkTable = new SqlCommand("SELECT OBJECT_ID(N'dbo.Discs', N'U')", connection)) {
            tableWasMissing = checkTable.ExecuteScalar() is DBNull;
        }
        using (var create = new SqlCommand("""
            IF OBJECT_ID(N'dbo.Discs', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Discs (
                    Code INT NOT NULL PRIMARY KEY,
                    Topic NVARCHAR(100) NOT NULL,
                    Title NVARCHAR(150) NOT NULL,
                    ReleaseYear INT NOT NULL CHECK (ReleaseYear BETWEEN 1900 AND 2100),
                    Price DECIMAL(10, 2) NOT NULL CHECK (Price >= 0)
                );
            END
            """, connection)) {
            create.ExecuteNonQuery();
        }

        // Не відновлюємо видалені користувачем дані на наступному запуску.
        if (!tableWasMissing) return;

        using var transaction = connection.BeginTransaction();
        
        {
            Disc[] samples = [
                new(1, "Програмування", "C# для початківців", 2020, 199.99m),
                new(2, "Музика", "Український джаз", 2019, 149.50m),
                new(3, "Освіта", "Алгебра: відеокурс", 2022, 125.00m),
                new(4, "Ігри", "Ретро-ігри: збірка", 2018, 299.00m),
                new(5, "Наука", "Космос і планети", 2023, 179.90m)
            ];
            foreach (var disc in samples) InsertInternal(connection, transaction, disc);
        }
        transaction.Commit();
    }

    public List<Disc> GetAll() {
        using var connection = Open();
        using var command = new SqlCommand("""
            SELECT Code, Topic, Title, ReleaseYear, Price
            FROM dbo.Discs
            ORDER BY Code
            """, connection);
        using var reader = command.ExecuteReader();
        List<Disc> discs = [];
        while (reader.Read()) {
            discs.Add(new Disc(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt32(3), reader.GetDecimal(4)));
        }
        return discs;
    }

    public int Insert(Disc disc) {
        using var connection = Open();
        return InsertInternal(connection, null, disc);
    }

    public int Update(int originalCode, Disc disc) {
        using var connection = Open();
        using var command = new SqlCommand("""
            UPDATE dbo.Discs
            SET Code = @code, Topic = @topic, Title = @title, ReleaseYear = @year, Price = @price
            WHERE Code = @originalCode
            """, connection);
        AddParameters(command, disc);
        command.Parameters.Add("@originalCode", SqlDbType.Int).Value = originalCode;
        return command.ExecuteNonQuery();
    }

    public int Delete(int code) {
        using var connection = Open();
        using var command = new SqlCommand("DELETE FROM dbo.Discs WHERE Code = @code", connection);
        command.Parameters.Add("@code", SqlDbType.Int).Value = code;
        return command.ExecuteNonQuery();
    }

    private SqlConnection Open() {
        var connection = new SqlConnection(_connectionString);
        try {
            connection.Open();
            return connection;
        } catch {
            connection.Dispose();
            throw;
        }
    }

    private static int InsertInternal(SqlConnection connection, SqlTransaction? transaction, Disc disc) {
        using var command = new SqlCommand("""
            INSERT INTO dbo.Discs (Code, Topic, Title, ReleaseYear, Price)
            VALUES (@code, @topic, @title, @year, @price)
            """, connection, transaction);
        AddParameters(command, disc);
        return command.ExecuteNonQuery();
    }

    private static void AddParameters(SqlCommand command, Disc disc) {
        command.Parameters.Add("@code", SqlDbType.Int).Value = disc.Code;
        command.Parameters.Add("@topic", SqlDbType.NVarChar, 100).Value = disc.Topic;
        command.Parameters.Add("@title", SqlDbType.NVarChar, 150).Value = disc.Title;
        command.Parameters.Add("@year", SqlDbType.Int).Value = disc.ReleaseYear;
        var price = command.Parameters.Add("@price", SqlDbType.Decimal);
        price.Precision = 10;
        price.Scale = 2;
        price.Value = disc.Price;
    }
}
