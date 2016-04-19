
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace VsEditions {
    /// <summary>
    /// Represents a unique edition of a known Visual Studio version,
    /// e.g "Visual Studio 2012 Ultimate".
    /// </summary>
    public class VisualStudioEdition {
        /// <summary>
        /// Contains the names of some Visual Studio editions.
        /// </summary>
        public static readonly string[] NamedEditions = new string[] {
            "Pro",
            "Premium",
            "Ultimate",
            "Community",
            "Enterprise"
        };

        /// <summary>
        /// Gets or sets the <see cref="VisualStudioVersion"/> this edition represent.
        /// </summary>
        public VisualStudioVersion Version { get; set; }

        /// <summary>
        /// Gets or sets the name of this edition, e.g. "Pro" (for "Professional")
        /// or "Ultimate".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the display name of this edition, e.g. "Professional" or
        /// "Premium".
        /// </summary>
        public string DisplayName {
            get {
                return Name.Equals("Pro", StringComparison.InvariantCultureIgnoreCase) ? "Professional" : Name;
            }
        }

        /// <summary>
        /// Gets or sets the installation directory of this edition.
        /// </summary>
        public string InstallationDirectory { get; set; }
    }

    /// <summary>
    /// Repesent a well-known Visual Studio version, i.e. "Visual Studio 2010".
    /// </summary>
    public class VisualStudioVersion {
        /// <summary>
        /// Represents the Visual Studio version 2010 ("10.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2010 = new VisualStudioVersion("2010", "10.0");

        /// <summary>
        /// Represents the Visual Studio version 2012 ("11.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2012 = new VisualStudioVersion("2012", "11.0");

        /// <summary>
        /// Represents the Visual Studio version 2013 ("12.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2013 = new VisualStudioVersion("2013", "12.0");

        /// <summary>
        /// Represents the Visual Studio version 2015 ("14.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2015 = new VisualStudioVersion("2015", "14.0");


        /// <summary>
        /// Initializes a <see cref="VisualStudioVersion"/>.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="number"></param>
        private VisualStudioVersion(string year, string number) {
            this.Year = year;
            this.BuildNumber = number;
        }

        /// <summary>
        /// Gets the year of this Visual Studio version, e.g "2010".
        /// </summary>
        public readonly string Year;

        /// <summary>
        /// Gets the build number of this Visual Studio version, e.g "10.0".
        /// </summary>
        public readonly string BuildNumber;
    }

    class Program {
        static void Main(string[] args) {
            var editions = GetInstalledVSEditionsSupported();
            foreach (var edition in editions) {
                Console.WriteLine("VS {0}/{1}: {2}", edition.Version.Year, edition.Version.BuildNumber, edition.DisplayName);
            }
        }

        /// <summary>
        /// Gets a list of Visual Studio editions we support integration with and
        /// that are installed on the current machine.
        /// </summary>
        /// <remarks>
        /// Implementation designed as suggested by this article:
        /// http://www.mztools.com/articles/2008/MZ2008003.aspx
        /// </remarks>
        /// <returns></returns>
        public static List<VisualStudioEdition> GetInstalledVSEditionsSupported() {
            List<VisualStudioEdition> installedEditions = new List<VisualStudioEdition>();
            VisualStudioVersion[] supportedVersions = new VisualStudioVersion[]
            {
                VisualStudioVersion.VS2010,
                VisualStudioVersion.VS2012,
                VisualStudioVersion.VS2013,
                VisualStudioVersion.VS2015
            };

            // No matter the OS, and despite us being a 64-bit application, we request
            // the 32-bit view of the registry, since this is where Visual Studio keep
            // it's settings (still being a 32-bit application).

            using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) {
                foreach (var version in supportedVersions) {
                    using (var versionKey = localMachine.OpenSubKey(string.Format(@"SOFTWARE\Microsoft\VisualStudio\{0}", version.BuildNumber))) {
                        if (versionKey == null)
                            continue;

                        using (var setupKey = versionKey.OpenSubKey(@"Setup\VS")) {
                            if (setupKey == null)
                                continue;

                            foreach (var namedEdition in VisualStudioEdition.NamedEditions) {
                                using (var editionKey = setupKey.OpenSubKey(namedEdition)) {
                                    if (editionKey == null)
                                        continue;

                                    // We have located an edition of a visual studio we support.
                                    // If we can resolve it's installation directory, we'll add
                                    // it to our list of supported editions we must consider
                                    // during installation.

                                    // Get the installation directory either from the specific
                                    // edition key, or from the embracing setup key, whichever
                                    // we find first.

                                    string installationDirectory = editionKey.GetValue("ProductDir") as string;
                                    if (string.IsNullOrEmpty(installationDirectory))
                                        installationDirectory = setupKey.GetValue("ProductDir") as string;

                                    if (!string.IsNullOrEmpty(installationDirectory)) {
                                        var edition = new VisualStudioEdition();
                                        edition.Version = version;
                                        edition.Name = namedEdition;
                                        edition.InstallationDirectory = installationDirectory;
                                        installedEditions.Add(edition);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return installedEditions;
        }
    }
}
