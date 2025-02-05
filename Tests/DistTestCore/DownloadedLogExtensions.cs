﻿using Core;
using NUnit.Framework;

namespace DistTestCore
{
    public static class DownloadedLogExtensions
    {
        public static void AssertLogContains(this IDownloadedLog log, string expectedString)
        {
            Assert.That(log.DoesLogContain(expectedString), $"Did not find '{expectedString}' in log.");
        }
    }
}
