using System;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Extends <see cref="AdapterOptionsMonitor{TAdapterOptions}"/> to use ASP.NET Core's 
    /// <see cref="IOptions{TOptions}"/> and <see cref="IOptionsMonitor{TOptions}"/> services to 
    /// supply adapter options.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The adapter options type.
    /// </typeparam>
    public sealed class AspNetCoreAdapterOptionsMonitor<TAdapterOptions> 
        : AdapterOptionsMonitor<TAdapterOptions>, IDisposable where TAdapterOptions : AdapterOptions, new() {

        /// <summary>
        /// The listener registration if an <see cref="IOptionsMonitor{TOptions}"/> is used to 
        /// provide options.
        /// </summary>
        private readonly IDisposable _listenerRegistration;


        /// <summary>
        /// Creates a new <see cref="AspNetCoreAdapterOptionsMonitor{TAdapterOptions}"/> object.
        /// </summary>
        /// <param name="options">
        ///   The ASP.NET Core <see cref="IOptions{TOptions}"/> service that supplies the adapter 
        ///   options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public AspNetCoreAdapterOptionsMonitor(IOptions<TAdapterOptions> options) 
            : base(options?.Value) { 
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
        }


        /// <summary>
        /// Creates a new <see cref="AspNetCoreAdapterOptionsMonitor{TAdapterOptions}"/> object 
        /// that will notify listeners when the adapter options are modified.
        /// </summary>
        /// <param name="optionsMonitor">
        ///   The ASP.NET Core <see cref="IOptionsMonitor{TOptions}"/> service that is used to 
        ///   supply the adapter options and notify about configuration changes.
        /// </param>
        /// <param name="optionsName">
        ///   The named options to use. Specify <see langword="null"/> or white space to use 
        ///   <see cref="Options.DefaultName"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> is <see langword="null"/>.
        /// </exception>
        public AspNetCoreAdapterOptionsMonitor(IOptionsMonitor<TAdapterOptions> optionsMonitor, string optionsName) 
            : base() {
            
            if (optionsMonitor == null) {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            if (string.IsNullOrWhiteSpace(optionsName)) {
                optionsName = Options.DefaultName;
            }

            CurrentValue = optionsMonitor.Get(optionsName);
            _listenerRegistration = optionsMonitor.OnChange((options, name) => { 
                if (!string.Equals(optionsName, name, StringComparison.Ordinal)) {
                    return;
                }

                CurrentValue = options;
            });
        }


        /// <summary>
        /// Releases managed resources.
        /// </summary>
        public void Dispose() {
            _listenerRegistration?.Dispose();
        }

    }
}
