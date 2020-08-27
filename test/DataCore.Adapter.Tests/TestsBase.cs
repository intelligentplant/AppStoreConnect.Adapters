using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    /// <summary>
    /// Base class for other test classes to inherit from.
    /// </summary>
    public abstract class TestsBase {

        /// <summary>
        /// The test context for the current test.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Cancellation token source that fires after every test.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// A cancellation token that will fire during test cleanup.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }


        /// <summary>
        /// Base test initializer.
        /// </summary>
        [TestInitialize]
        public virtual void Initialize() {
            _cancellationTokenSource = new CancellationTokenSource();
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

    }
}
