namespace lab08.Data;


/// <summary>Один запис каталогу, що відповідає рядку таблиці dbo.Discs.</summary>
internal sealed record Disc(int Code, string Topic, string Title, int ReleaseYear, decimal Price);
