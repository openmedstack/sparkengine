// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace OpenMedStack.SparkEngine.Service.FhirServiceExtensions
{
    using System.Threading.Tasks;

    internal class IndexRebuildProgress
    {
        private const int IndexClearProgressPercentage = 10;

        private readonly IIndexBuildProgressReporter? _reporter;
        private int _overallProgress;
        private int _recordsProcessed;
        private int _remainingProgress = 100;

        public IndexRebuildProgress(IIndexBuildProgressReporter? reporter) => _reporter = reporter;

        public async Task Started()
        {
            await ReportProgress("Index rebuild started").ConfigureAwait(false);
        }

        public async Task CleanStarted()
        {
            await ReportProgress("Clearing index").ConfigureAwait(false);
        }

        public async Task CleanCompleted()
        {
            _overallProgress += IndexClearProgressPercentage;
            await ReportProgress("Index cleared").ConfigureAwait(false);
            _remainingProgress -= _overallProgress;
        }

        public async Task RecordsProcessed(int records, long total)
        {
            _recordsProcessed += records;
            _overallProgress += (int) (_remainingProgress / (double) total * records);
            await ReportProgress($"{_recordsProcessed} records processed").ConfigureAwait(false);
        }

        public async Task Done()
        {
            _overallProgress = 100;
            await ReportProgress("Index rebuild done").ConfigureAwait(false);
        }

        public async Task Error(string error)
        {
            if (_reporter == null)
            {
                return;
            }

            await _reporter.ReportError(error).ConfigureAwait(false);
        }

        private async Task ReportProgress(string message)
        {
            if (_reporter == null)
            {
                return;
            }

            await _reporter.ReportProgress(_overallProgress, message).ConfigureAwait(false);
        }
    }
}
