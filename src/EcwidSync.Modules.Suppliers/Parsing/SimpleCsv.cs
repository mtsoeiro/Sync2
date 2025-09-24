namespace EcwidSync.Modules.Suppliers.Parsing;

internal static class SimpleCsv
{
    // split básico que respeita aspas
    public static string[] Split(string line, char sep)
    {
        var list = new List<string>();
        bool inQ = false;
        var cur = new System.Text.StringBuilder();

        foreach (var ch in line)
        {
            if (ch == '"') { inQ = !inQ; cur.Append(ch); }
            else if (ch == sep && !inQ) { list.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        list.Add(cur.ToString());
        return list.ToArray();
    }
}
