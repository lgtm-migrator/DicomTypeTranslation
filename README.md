# DicomTypeTranslation
FoDicom/FAnsiSql powered library for converting dicom types into database/C# types at speed.

# Reading

One goal of this library is high speed reading of strongly typed values from dicom tags.  This is handled by the static class `DicomTypeTranslaterReader`.

Consider the following example dataset (FoDicom):

```csharp
var ds = new DicomDataset(new List<DicomItem>()
{
    new DicomShortString(DicomTag.PatientName,"Frank"),
    new DicomAgeString(DicomTag.PatientAge,"032Y"),
    new DicomDate(DicomTag.PatientBirthDate,new DateTime(2001,1,1))
});
```

We can read all these as follows:

```csharp
object name = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
Assert.AreEqual(typeof(string),name.GetType());
Assert.AreEqual("Frank",name);

object age = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientAge);
Assert.AreEqual(typeof(string),age.GetType());
Assert.AreEqual("032Y",age);

object dob = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientBirthDate);
Assert.AreEqual(typeof(DateTime),dob.GetType());
Assert.AreEqual(new DateTime(2001,01,01), dob);
```

## Multiplicity

The Dicom specification allows multiple elements to be specified for some tags, this is called 'multiplicity':

```csharp
//create an Fo-Dicom dataset with string multiplicity
var ds = new DicomDataset(new List<DicomItem>()
{
    new DicomShortString(DicomTag.PatientName,"Frank","Anderson")
});
```

We represent multiplicity as arrays:

```csharp

object name2 = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
Assert.AreEqual(typeof(string[]),name2.GetType());
Assert.AreEqual(new string[]{"Frank","Anderson"},name2);
```

If you don't want to deal with arrays you can flatten the result:

```csharp
name2 = DicomTypeTranslater.Flatten(name2);
Assert.AreEqual(typeof(string),name2.GetType());
Assert.AreEqual("Frank\\Anderson",name2);
```

## Sequences

The Dicom specification allows trees too, these are called Sequences.  A Sequence consists of 1 or more sub datasets.

```csharp
//The top level dataset
var ds = new DicomDataset(new List<DicomItem>()
{
    //top level dataset has a normal tag
    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID,"1.2.3"), 

    //and a sequence tag
    new DicomSequence(DicomTag.ActualHumanPerformersSequence,new []
    {
        //sequnce tag is composed of two sub trees:
        //subtree 1
        new DicomDataset(new List<DicomItem>()
        {
            new DicomShortString(DicomTag.PatientName,"Rabbit")
        }), 

        //subtree 2
        new DicomDataset(new List<DicomItem>()
        {
            new DicomShortString(DicomTag.PatientName,"Roger")
        })
    })
});
```

We represent sequences as trees (`Dictionary<DicomTag,object>`):

```csharp
var seq = (Dictionary<DicomTag,object>[])DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.ActualHumanPerformersSequence);
Assert.AreEqual("Rabbit",seq[0][DicomTag.PatientName]);
Assert.AreEqual("Roger",seq[1][DicomTag.PatientName]);
```

Again if you don't want to deal with this you can just call Flatten:

```csharp
var flattened = DicomTypeTranslater.Flatten(seq);
```

The Flattened (string) representation of the above example is:
```
[0] - 
 	 (0010,0010) - 	 Rabbit

[1] - 
 	 (0010,0010) - 	 Roger
```

## Database Types

The Dicom specification has rules about how big datatypes can be (called ValueRepresentations) for example the entry for [Patient Address](http://northstar-www.dartmouth.edu/doc/idl/html_6.2/DICOM_Attributes.html) is LO ("Long String") which has a maximum length of 64 charactesr.

```csharp
var tag = DicomDictionary.Default["PatientAddress"];            
```

The library supports translating DicomTags into the matching [FAnsiSql](https://github.com/HicServices/FAnsiSql) common type representation:

```csharp
DatabaseTypeRequest type = DicomTypeTranslater.GetNaturalTypeForVr(tag.DictionaryEntry.ValueRepresentations,tag.DictionaryEntry.ValueMultiplicity);

Assert.AreEqual(typeof(string),type.CSharpType);
Assert.AreEqual(64,type.MaxWidthForStrings);
```

This `DataTypeRequest` can then be converted to the appropriate database column type:

```csharp
TypeTranslater tt = new MicrosoftSQLTypeTranslater();
Assert.AreEqual("varchar(64)",tt.GetSQLDBTypeForCSharpType(type));

tt = new OracleTypeTranslater();
Assert.AreEqual("varchar2(64)",tt.GetSQLDBTypeForCSharpType(type));
```

This lets you build adhoc database schemas in any DBMS (supported by FAnsi) based on arbitrary dicom tags picked by your users.
