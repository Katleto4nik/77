using System.Text;

namespace RecentFiles;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        int hours = 10;
        string exts = "pdf,exe,dll,png,jar";
        bool all = false, app = true, sys = false, access = true, popular = false, scan = false, modsJars = false;

        // Необов'язкові параметри: --hours 24 --ext pdf,exe --scan --all --noappdata --system --popular --modsjars --noaccess
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i].ToLowerInvariant();
            if (a == "--hours" && i + 1 < args.Length && int.TryParse(args[i + 1], out var h) && h > 0) { hours = h; i++; }
            else if (a == "--ext" && i + 1 < args.Length) { exts = args[++i]; }
            else if (a == "--all") { all = true; scan = true; }
            else if (a == "--scan") scan = true;
            else if (a == "--appdata") app = true;
            else if (a == "--noappdata") app = false;
            else if (a == "--system") sys = true;
            else if (a == "--popular") popular = true;
            else if (a == "--modsjars") modsJars = true;
            else if (a == "--noaccess") access = false;
        }

        var set = new HashSet<string>(
            exts.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.TrimStart('.')),
            StringComparer.OrdinalIgnoreCase);

        var o = new Options
        {
            Hours = hours,
            Exts = set,
            AllDrives = all,
            IncludeAppData = app,
            IncludeSystem = sys,
            IncludePopular = popular,
            IncludeModsJars = modsJars,
            UseScan = scan,
            UseAccessTime = access
        };

        Console.WriteLine($"Файли ({string.Join(", ", set)}) за останні {hours} год.");
        Console.WriteLine(scan
            ? (all ? "Режим: Recent + UserAssist + скан усіх дисків" : "Режим: Recent + UserAssist + скан папки користувача")
            : "Режим: лише те, що відкрив сам користувач (Recent + UserAssist)");
        Console.WriteLine("Сканую, зачекай...");
        Console.WriteLine();

        var list = Collector.Collect(o, CancellationToken.None);

        foreach (var e in list)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(e.Time.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  [{e.Kind}/{e.Source}]  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Path.GetFileName(e.FullPath));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    -> " + (Path.GetDirectoryName(e.FullPath) ?? e.FullPath));
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine($"Усього: {list.Count}");

        var perExt = string.Join(", ", set.Select(x =>
            $"{x}: {list.Count(e => Path.GetExtension(e.FullPath).TrimStart('.').Equals(x, StringComparison.OrdinalIgnoreCase))}"));
        Console.WriteLine($"По типах -> {perExt}");

        var perSrc = string.Join(", ", Collector.SourceCounts.Select(kv => $"{kv.Key}={kv.Value}"));
        Console.WriteLine($"Джерела  -> {(perSrc.Length == 0 ? "порожньо" : perSrc)}");
        if (Collector.RecentError != null)
            Console.WriteLine($"Помилка Recent: {Collector.RecentError}");
        Console.WriteLine("Натисни будь-яку клавішу, щоб закрити...");
        try { Console.ReadKey(true); } catch { }
        return 0;
    }
}
