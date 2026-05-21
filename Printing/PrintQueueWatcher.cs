using System.Printing;

namespace PrintFraItslearning.Printing;

public static class PrintQueueWatcher
{
    public static void WaitForQueueDrained(string printerName,
        int maxWaitMs = 60_000,
        int pollMs = 500,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(printerName)) return;
        try
        {
            using var server = new LocalPrintServer();
            using var queue = TryGetQueue(server, printerName);
            if (queue == null) return;

            var deadline = DateTime.UtcNow.AddMilliseconds(maxWaitMs);
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                queue.Refresh();
                if (queue.NumberOfJobs <= 0) return;
                Thread.Sleep(pollMs);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Stille — kø-overvåking er beste-innsats
        }
    }

    private static PrintQueue? TryGetQueue(LocalPrintServer server, string printerName)
    {
        try
        {
            // Strip evt. \\server\ prefiks for å finne lokalt navn
            var local = printerName;
            return server.GetPrintQueue(local);
        }
        catch
        {
            // Søk gjennom alle køer
            try
            {
                foreach (var q in server.GetPrintQueues())
                {
                    if (q.FullName.Equals(printerName, StringComparison.OrdinalIgnoreCase) ||
                        q.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                        return q;
                }
            }
            catch { }
            return null;
        }
    }
}
