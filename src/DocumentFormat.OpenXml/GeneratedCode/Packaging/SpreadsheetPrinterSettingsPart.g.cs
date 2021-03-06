// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DocumentFormat.OpenXml.Framework;
using System;
using System.Collections.Generic;

namespace DocumentFormat.OpenXml.Packaging
{
    /// <summary>
    /// Defines the SpreadsheetPrinterSettingsPart
    /// </summary>
    [ContentType(ContentTypeConstant)]
    [RelationshipTypeAttribute(RelationshipTypeConstant)]
    public partial class SpreadsheetPrinterSettingsPart : OpenXmlPart, IFixedContentTypePart
    {
        internal const string ContentTypeConstant = "application/vnd.openxmlformats-officedocument.spreadsheetml.printerSettings";
        internal const string RelationshipTypeConstant = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/printerSettings";

        /// <summary>
        /// Creates an instance of the SpreadsheetPrinterSettingsPart OpenXmlType
        /// </summary>
        internal protected SpreadsheetPrinterSettingsPart()
        {
        }

        /// <inheritdoc/>
        public sealed override string ContentType => ContentTypeConstant;

        /// <inheritdoc/>
        public sealed override string RelationshipType => RelationshipTypeConstant;

        /// <inheritdoc/>
        internal sealed override string TargetFileExtension => ".bin";

        /// <inheritdoc/>
        internal sealed override string TargetName => "printerSettings";

        /// <inheritdoc/>
        internal sealed override string TargetPath => "../printerSettings";
    }
}
