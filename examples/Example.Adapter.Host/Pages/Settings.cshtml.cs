using System.Text.Json;

using DataCore.Adapter;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Example.Adapter.Host.Pages {

    [ValidateAntiForgeryToken]
    public class SettingsModel : PageModel {

        private static readonly SemaphoreSlim s_updateModelLock = new SemaphoreSlim(1, 1);

        public IAdapter Adapter { get; }

        [BindProperty]
        public RngAdapterOptions? Options { get; set; }

        private readonly IOptionsMonitor<RngAdapterOptions> _optionsMonitor;

        public SettingsModel(IAdapter adapter, IOptionsMonitor<RngAdapterOptions> optionsMonitor) {
            Adapter = adapter;
            _optionsMonitor = optionsMonitor;
        }

        public void OnGet() {
            Options = _optionsMonitor.Get(Constants.AdapterId);
        }


        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return Page();
            }

            if (Options != null) {
                await s_updateModelLock.WaitAsync(cancellationToken);
                try {
                    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    using (_optionsMonitor.OnChange((opts, key) => { tcs.TrySetResult(); })) {
                        using (var stream = System.IO.File.Open(Constants.AdapterSettingsFilePath, FileMode.Create, FileAccess.Write)) {
                            var jsonOptions = new JsonSerializerOptions() {
                                WriteIndented = true
                            };

                            await JsonSerializer.SerializeAsync(stream, new {
                                AppStoreConnect = new {
                                    Adapter = new {
                                        Settings = Options
                                    }
                                }
                            }, jsonOptions);

                            
                        }

                        await tcs.Task.WaitAsync(cancellationToken);
                    }
                }
                finally {
                    s_updateModelLock.Release();
                }
            }

            return Redirect("/");
        }

    }
}
