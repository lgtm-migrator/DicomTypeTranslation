﻿
using Dicom;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;
using NLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Tests
{
    [TestFixture]
    public class DicomTypeTranslatorTests
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        #endregion

        #region Tests

        [Test]
        public void TestBasicCSharpTranslation()
        {
            DicomDataset ds = TranslationTestHelpers.BuildVrDataset();

            foreach (DicomItem item in ds)
                Assert.NotNull(DicomTypeTranslaterReader.GetCSharpValue(ds, item));
        }

        [Test]
        public void TestSequenceConversion()
        {
            var subDataset = new DicomDataset
            {
                new DicomShortString(DicomTag.SpecimenShortDescription, "short desc"),
                new DicomAgeString(DicomTag.PatientAge, "99Y")
            };

            var ds = new DicomDataset
            {
                new DicomSequence(DicomTag.ReferencedImageSequence, subDataset)
            };

            object obj = DicomTypeTranslaterReader.GetCSharpValue(ds, ds.GetDicomItem<DicomItem>(DicomTag.ReferencedImageSequence));

            var asArray = obj as Dictionary<DicomTag, object>[];
            Assert.NotNull(asArray);

            Assert.AreEqual(1, asArray.Length);
            Assert.AreEqual(2, asArray[0].Count);

            Assert.AreEqual("short desc", asArray[0][DicomTag.SpecimenShortDescription]);
            Assert.AreEqual("99Y", asArray[0][DicomTag.PatientAge]);
        }

        [Test]
        public void TestWriteMultiplicity()
        {
            DicomTag stringMultiTag = DicomTag.SpecimenShortDescription;
            string[] values = { "this", "is", "a", "multi", "element", "" };

            var ds = new DicomDataset();

            DicomTypeTranslaterWriter.SetDicomTag(ds, stringMultiTag, values);

            Assert.AreEqual(1, ds.Count());
            Assert.AreEqual(6, ds.GetDicomItem<DicomElement>(stringMultiTag).Count);
            Assert.AreEqual("this\\is\\a\\multi\\element\\", ds.GetString(stringMultiTag));
        }

        [Test]
        public void TestMultipleElementSequences()
        {
            var subDatasets = new List<DicomDataset>();
            for (var i = 0; i < 3; i++)
            {
                subDatasets.Add(new DicomDataset
                {
                    {DicomTag.ReferencedSOPClassUID, "ReferencedSOPClassUID-" + (i + 1)},
                    {DicomTag.ReferencedSOPInstanceUID, "ReferencedSOPInstanceUID-" + (i + 1)}
                });
            }

            var originalDataset = new DicomDataset
            {
                {DicomTag.ReferencedImageSequence, subDatasets.ToArray()}
            };

            var translatedDataset = new Dictionary<DicomTag, object>();

            foreach (DicomItem item in originalDataset)
            {
                object value = DicomTypeTranslaterReader.GetCSharpValue(originalDataset, item);
                translatedDataset.Add(item.Tag, value);
            }

            var reconstructedDataset = new DicomDataset();

            foreach (KeyValuePair<DicomTag, object> item in translatedDataset)
                DicomTypeTranslaterWriter.SetDicomTag(reconstructedDataset, item.Key, item.Value);

            Assert.True(DicomDatasetHelpers.ValueEquals(originalDataset, reconstructedDataset));
        }

        [Test]
        public void TestSetDicomTagWithNullElement()
        {
            var dataset = new DicomDataset();

            // Test with a string element and a value element
            var asTag = DicomTag.SelectorASValue;
            var flTag = DicomTag.SelectorFLValue;

            DicomTypeTranslaterWriter.SetDicomTag(dataset, asTag, null);
            DicomTypeTranslaterWriter.SetDicomTag(dataset, flTag, null);

            Assert.True(dataset.Count() == 2);

            var asElement = dataset.GetDicomItem<DicomElement>(DicomTag.SelectorASValue);
            Assert.True(asElement.Buffer.Size == 0);

            var flElement = dataset.GetDicomItem<DicomElement>(DicomTag.SelectorFLValue);
            Assert.True(flElement.Buffer.Size == 0);
        }

        [Test]
        public void Test_Sequence()
        {
            var subDatasets = new List<DicomDataset>
            {
                new DicomDataset
                {
                    new DicomShortString(DicomTag.CodeValue, "CPELVD")
                }
            };

            var dicomDataset = new DicomDataset
            {
                {DicomTag.ProcedureCodeSequence, subDatasets.ToArray()}
            };

            object result = DicomTypeTranslaterReader.GetCSharpValue(dicomDataset, DicomTag.ProcedureCodeSequence);


            object flat = DicomTypeTranslater.Flatten(result);
            Console.WriteLine(flat);

            StringAssert.Contains("CPELVD", (string)flat);
            StringAssert.Contains("(0008,0100)", (string)flat);
        }

        [Test]
        public void TestPatientAgeTag()
        {
            var dataset = new DicomDataset();

            dataset.Add(new DicomAgeString(DicomTag.PatientAge, "009Y"));

            var cSharpValue = DicomTypeTranslaterReader.GetCSharpValue(dataset, DicomTag.PatientAge);

            Assert.AreEqual("009Y", cSharpValue);
        }


        [Test]
        public void PrintValueTypesForVrs()
        {
            DicomVR[] vrs = TranslationTestHelpers.AllVrCodes;
            var uniqueTypes = new SortedSet<string>();

            foreach (DicomVR vr in vrs)
            {
                //SQ value representation doesn't have ValueType defined
                if (vr == DicomVR.SQ)
                    continue;

                _logger.Info("VR: " + vr.Code + "\t Type: " + vr.ValueType.Name + "\t IsString: " + vr.IsString);
                uniqueTypes.Add(vr.ValueType.Name.TrimEnd(']', '['));
            }

            var sb = new StringBuilder();
            foreach (string str in uniqueTypes)
                sb.Append(str + ", ");

            sb.Length -= 2;
            _logger.Info("Unique underlying types: " + sb);
        }

        [Test]
        public void TestGetCSharpValueThrowsException()
        {
            var ds = new DicomDataset
            {
                new DicomDecimalString(DicomTag.SelectorDSValue, "aaahhhhh")
            };

            Assert.Throws<FormatException>(() => DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.SelectorDSValue));
        }

        #endregion
    }
}
