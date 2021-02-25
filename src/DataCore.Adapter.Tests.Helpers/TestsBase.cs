using System;
using System.Globalization;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    /// <summary>
    /// Base class that other test classes can inherit from.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Resources are disposed during test cleanup.")]
    public abstract class TestsBase {

        /// <summary>
        /// The context for the current test.
        /// </summary>
        public TestContext TestContext { get; set; } = default!;


        /// <summary>
        /// Cancellation token source that fires after every test.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = default!;

        /// <summary>
        /// A cancellation token that will fire during test cleanup.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }


        /// <summary>
        /// Base test initializer.
        /// </summary>
        [TestInitialize]
        public virtual void Initialize() {
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            CancellationToken = _cancellationTokenSource.Token;
        }


        /// <summary>
        /// Base test cleanup.
        /// </summary>
        [TestCleanup]
        public virtual void Cleanup() {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }


        /// <summary>
        /// Cancels the <see cref="CancellationToken"/> for the test.
        /// </summary>
        public void Cancel() {
            _cancellationTokenSource?.Cancel();
        }


        /// <summary>
        /// Cancels the <see cref="CancellationToken"/> for the test after the specified 
        /// <paramref name="delay"/>.
        /// </summary>
        /// <param name="delay">
        ///   The delay.
        /// </param>
        public void CancelAfter(TimeSpan delay) {
            _cancellationTokenSource?.CancelAfter(delay);
        }


        /// <summary>
        /// Shorthand for calling <see cref="string.Format(IFormatProvider, string, object[])"/> 
        /// using the current culture.
        /// </summary>
        /// <param name="format">
        ///   A composite format string.
        /// </param>
        /// <param name="args">
        ///   An array that contains objects to format.
        /// </param>
        /// <returns>
        ///   The formatted string.
        /// </returns>
        public string FormatMessage(string format, params object[] args) {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

    }
}
