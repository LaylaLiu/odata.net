using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Values;
using Microsoft.OData.Edm.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GenModel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var model = GenerateModel();
            Print(model, "Proxy.xml");
        }

        public static Func<IEdmComplexType, bool, EdmCollectionTypeReference> CreateCollectionComplexTypeReference = (ct, nullable)
            => new EdmCollectionTypeReference(new EdmCollectionType(new EdmComplexTypeReference(ct, nullable)));
        public static Func<IEdmEnumType, bool, EdmCollectionTypeReference> CreateCollectionEnumTypeReference = (et, nullable)
            => new EdmCollectionTypeReference(new EdmCollectionType(new EdmEnumTypeReference(et, nullable)));
        public static Func<IEdmPrimitiveTypeReference, EdmCollectionTypeReference> CreateCollectionTypeReferenceForPrimitive = (pt)
            => new EdmCollectionTypeReference(new EdmCollectionType(pt));

        public static IEdmModel GenerateModel()
        {
            string ns = "NS";

            EdmModel model = new EdmModel();
            var defaultContainer = new EdmEntityContainer(ns, "DefaultContainer");
            model.AddElement(defaultContainer);

            #region EnumType
            var enumType = new EdmEnumType(ns, "EnumT", isFlags: false);
            enumType.AddMember("EnumP1", new EdmIntegerConstant(0));
            enumType.AddMember("EnumP2", new EdmIntegerConstant(1));
            enumType.AddMember("EnumP3", new EdmIntegerConstant(2));
            model.AddElement(enumType);
            #endregion

            #region ComplexType
            var abstractCT = new EdmComplexType(ns, "AbstractCT", null, true);
            model.AddElement(abstractCT);

            var abstractBaseCT = new EdmComplexType(ns, "AbstractBaseCT", abstractCT, true);
            abstractBaseCT.AddProperty(new EdmStructuralProperty(abstractBaseCT, "CP0", EdmCoreModel.Instance.GetInt32(false)));
            model.AddElement(abstractBaseCT);

            var baseCT = new EdmComplexType(ns, "BaseCT", abstractBaseCT);
            baseCT.AddProperty(new EdmStructuralProperty(baseCT, "CP1", EdmCoreModel.Instance.GetInt32(false)));
            model.AddElement(baseCT);

            var derivedCT = new EdmComplexType(ns, "CT", baseCT);
            derivedCT.AddProperty(new EdmStructuralProperty(derivedCT, "CP2", EdmCoreModel.Instance.GetInt32(false)));
            model.AddElement(derivedCT);
            #endregion

            #region EntityType
            var abstractET = new EdmEntityType(ns, "AbstractET", null, true, false);
            model.AddElement(abstractET);

            var abstractBaseET = new EdmEntityType(ns, "AbstractBaseET", abstractET, true, false);
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "EP0", EdmCoreModel.Instance.GetInt32(false)));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "EAbsCTP", new EdmComplexTypeReference(abstractCT, false)));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "EColOfAbsCTP", CreateCollectionComplexTypeReference(abstractCT, false)));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "EColOfNullableAbsCTP", CreateCollectionComplexTypeReference(abstractCT, true)));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "EColOfNullableInt", CreateCollectionTypeReferenceForPrimitive(EdmCoreModel.Instance.GetInt32(true))));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "EColOfInt", CreateCollectionTypeReferenceForPrimitive(EdmCoreModel.Instance.GetInt32(false))));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "ENullableEnum", new EdmEnumTypeReference(enumType, true)));
            abstractBaseET.AddProperty(new EdmStructuralProperty(abstractBaseET, "ENullableAbsCTP", new EdmComplexTypeReference(abstractCT, true)));
            model.AddElement(abstractBaseET);

            var baseET = new EdmEntityType(ns, "BaseET", abstractBaseET, false, false);
            var baseETKey = new EdmStructuralProperty(baseET, "EK0", EdmCoreModel.Instance.GetInt32(false));
            baseET.AddKeys(baseETKey);
            baseET.AddProperty(baseETKey);
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EP1", EdmCoreModel.Instance.GetInt32(false)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EAbsBaseCTP", new EdmComplexTypeReference(abstractBaseCT, false)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EBaseCTP", new EdmComplexTypeReference(baseCT, false)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EColOfAbsBaseCTP", CreateCollectionComplexTypeReference(abstractBaseCT, false)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EColOfBaseCTP", CreateCollectionComplexTypeReference(baseCT, false)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EColOfNullableAbsBaseCTP", CreateCollectionComplexTypeReference(abstractBaseCT, true)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EColOfNullableBaseCTP", CreateCollectionComplexTypeReference(baseCT, true)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "EColOfEnum", CreateCollectionEnumTypeReference(enumType, false)));
            baseET.AddProperty(new EdmStructuralProperty(baseET, "ENullablePrimitive", EdmCoreModel.Instance.GetInt32(true)));
            model.AddElement(baseET);

            var derivedET = new EdmEntityType(ns, "ET", baseET, false, true);
            derivedET.AddProperty(new EdmStructuralProperty(derivedET, "EP2", EdmCoreModel.Instance.GetInt32(false)));
            derivedET.AddProperty(new EdmStructuralProperty(derivedET, "EDerivedCTP", new EdmComplexTypeReference(derivedCT, false)));
            derivedET.AddProperty(new EdmStructuralProperty(derivedET, "EColOfDerivedCTP", CreateCollectionComplexTypeReference(derivedCT, false)));
            derivedET.AddProperty(new EdmStructuralProperty(derivedET, "EColOfNullableEnum", CreateCollectionEnumTypeReference(enumType, true)));
            model.AddElement(derivedET);
            #endregion

            #region Bound Function            
            // Bound to Entity, Return Collection of Abstract Complex Type
            var boundETGetColOfAbsCT = new EdmFunction(ns, "BETRColOfAbsCT", CreateCollectionComplexTypeReference(abstractCT, false), true, null, false);
            boundETGetColOfAbsCT.AddParameter("entity", new EdmEntityTypeReference(baseET, false));
            model.AddElement(boundETGetColOfAbsCT);

            // Bound to Entity, Return Collection of Abstract Base Complex Type
            var boundETGetColOfAbsBaseCT = new EdmFunction(ns, "BETRColOfAbsBaseCT", CreateCollectionComplexTypeReference(abstractBaseCT, false), true, null, true);
            boundETGetColOfAbsBaseCT.AddParameter("entity", new EdmEntityTypeReference(baseET, false));
            model.AddElement(boundETGetColOfAbsBaseCT);

            // Bound to Entity, Return Collection of Base Complex Type
            var boundETGetColOfBaseCT = new EdmFunction(ns, "BETRColOfBaseCT", CreateCollectionComplexTypeReference(baseCT, false), true, null, false);
            boundETGetColOfBaseCT.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetColOfBaseCT);

            // Bound to Entity, Return Collection of Dervied Complex Type
            var boundETGetColOfDerivedCT = new EdmFunction(ns, "BETRColOfDerivedCT", CreateCollectionComplexTypeReference(derivedCT, false), true, null, true);
            boundETGetColOfDerivedCT.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetColOfDerivedCT);

            // Bound to Entity, Return Collection of Nullable Abstract Complex Type
            var boundETGetColOfNullableAbsCT = new EdmFunction(ns, "BETRColOfNullableAbsCT", CreateCollectionComplexTypeReference(abstractCT, true), true, null, false);
            boundETGetColOfNullableAbsCT.AddParameter("entity", new EdmEntityTypeReference(baseET, false));
            model.AddElement(boundETGetColOfNullableAbsCT);

            // Bound to Entity, Return Collection of Nullable Base Complex Type
            var boundETGetColOfNullableBaseCT = new EdmFunction(ns, "BETRColOfNullableBaseCT", CreateCollectionComplexTypeReference(baseCT, true), true, null, false);
            boundETGetColOfNullableBaseCT.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetColOfNullableBaseCT);

            // Bound to Entity, Return Collection of Nullable Primitive Type
            var boundETGetColOfNullableInt = new EdmFunction(ns, "BETRColOfNullableInt", CreateCollectionTypeReferenceForPrimitive(EdmCoreModel.Instance.GetInt32(true)), true, null, false);
            boundETGetColOfNullableInt.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetColOfNullableInt);

            // Bound to Entity, Return Collection of Primitive Type
            var boundETGetColOfInt = new EdmFunction(ns, "BETRColOfInt", CreateCollectionTypeReferenceForPrimitive(EdmCoreModel.Instance.GetInt32(false)), true, null, false);
            boundETGetColOfInt.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetColOfInt);

            // Bound to Entity, Return Collection of Nullable Enum
            var boundETGetColOfNullableEnum = new EdmFunction(ns, "BETRColOfNullableEnum", CreateCollectionEnumTypeReference(enumType, true), true, null, false);
            boundETGetColOfNullableEnum.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetColOfNullableEnum);

            // Bound to Entity, Return Collection of Enum
            var boundETGetColOfEnum = new EdmFunction(ns, "BETRColOfEnum", CreateCollectionEnumTypeReference(enumType, false), true, null, false);
            boundETGetColOfEnum.AddParameter("entity", new EdmEntityTypeReference(baseET, false));
            model.AddElement(boundETGetColOfEnum);

            // Bound to Entity, Return Nullable Enum
            var boundETGetNullableEnum = new EdmFunction(ns, "BETRNullableEnum", new EdmEnumTypeReference(enumType, true), true, null, false);
            boundETGetNullableEnum.AddParameter("entity", new EdmEntityTypeReference(derivedET, false));
            model.AddElement(boundETGetNullableEnum);
            #endregion

            #region UnBound Function
            // Return Collection of Abstract Complex Type
            var getColOfAbsCT = new EdmFunction(ns, "RColOfAbsCT", CreateCollectionComplexTypeReference(abstractCT, false), false, null, false);
            model.AddElement(getColOfAbsCT);

            // Return Collection of Abstract Base Complex Type
            var getColOfAbsBaseCT = new EdmFunction(ns, "RColOfAbsBaseCT", CreateCollectionComplexTypeReference(abstractBaseCT, false), false, null, true);
            model.AddElement(getColOfAbsBaseCT);

            // Return Collection of Base Complex Type
            var getColOfBaseCT = new EdmFunction(ns, "RColOfBaseCT", CreateCollectionComplexTypeReference(baseCT, false), false, null, false);
            model.AddElement(getColOfBaseCT);

            // Return Collection of Dervied Complex Type
            var getColOfDerivedCT = new EdmFunction(ns, "RColOfDerivedCT", CreateCollectionComplexTypeReference(derivedCT, false), false, null, true);
            model.AddElement(getColOfDerivedCT);

            // Return Collection of Abstract Complex Type
            var getColOfNullableAbsCT = new EdmFunction(ns, "RColOfNullableAbsCT", CreateCollectionComplexTypeReference(abstractCT, true), false, null, false);
            model.AddElement(getColOfNullableAbsCT);

            // Return Collection of Base Complex Type
            var getColOfNullableBaseCT = new EdmFunction(ns, "RColOfNullableBaseCT", CreateCollectionComplexTypeReference(baseCT, true), false, null, false);
            model.AddElement(getColOfNullableBaseCT);

            // Return Collection of Nullable Primitive Type
            var getColOfNullableInt = new EdmFunction(ns, "RColOfNullableInt", CreateCollectionTypeReferenceForPrimitive(EdmCoreModel.Instance.GetInt32(true)), false, null, false);
            model.AddElement(getColOfNullableInt);

            // Return Collection of Base Complex Type
            var getColOfInt = new EdmFunction(ns, "RColOfInt", CreateCollectionTypeReferenceForPrimitive(EdmCoreModel.Instance.GetInt32(false)), false, null, false);
            model.AddElement(getColOfInt);
            #endregion

            #region EntityContainer
            var abstractETs = new EdmEntitySet(defaultContainer, "AbstractETs", abstractET);
            defaultContainer.AddElement(abstractETs);

            var abstractBaseETs = new EdmEntitySet(defaultContainer, "AbstractBaseETs", abstractBaseET);
            defaultContainer.AddElement(abstractBaseETs);

            var baseETs = new EdmEntitySet(defaultContainer, "BaseETs", baseET);
            defaultContainer.AddElement(baseETs);

            var derivedETs = new EdmEntitySet(defaultContainer, "DerivedETs", derivedET);
            defaultContainer.AddElement(derivedETs);

            defaultContainer.AddFunctionImport(getColOfAbsCT.Name, getColOfAbsCT);
            defaultContainer.AddFunctionImport(getColOfAbsBaseCT.Name, getColOfAbsBaseCT);
            defaultContainer.AddFunctionImport(getColOfBaseCT.Name, getColOfBaseCT);
            defaultContainer.AddFunctionImport(getColOfDerivedCT.Name, getColOfDerivedCT);
            defaultContainer.AddFunctionImport(getColOfNullableAbsCT.Name, getColOfNullableAbsCT);
            defaultContainer.AddFunctionImport(getColOfNullableBaseCT.Name, getColOfNullableBaseCT);
            defaultContainer.AddFunctionImport(getColOfNullableInt.Name, getColOfNullableInt);
            defaultContainer.AddFunctionImport(getColOfInt.Name, getColOfInt);
            #endregion

            return model;
        }

        static void Print(IEdmModel model, string metadataName)
        {
            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = System.Text.Encoding.UTF8;
            XmlWriter xw = XmlWriter.Create(sw, settings);
            IEnumerable<EdmError> errors;
            EdmxWriter.TryWriteEdmx(model, xw, EdmxTarget.OData, out errors);
            xw.Flush();
            xw.Close();
            File.WriteAllText(metadataName, sw.ToString());
        }
    }
}
