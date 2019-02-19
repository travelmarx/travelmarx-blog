// ExtractComments.cpp : Defines the entry point for the console application.

#include "stdafx.h"
#include "stdio.h"
#include "windows.h"
#include "shlobj.h"
#include <iostream>
//#include "util.h"
using namespace std;

// Thanks to https://code.msdn.microsoft.com/Outlook-2010-Building-a-C-0358a023/sourcecode?fileId=25847&pathId=1202554927
//
class AutoVariant :
    public VARIANT
{

public:

    AutoVariant()
    {
        VariantInit(this);
    }

    ~AutoVariant()
    {
        VariantClear(this);
    }

    HRESULT
        SetBSTRValue(
            LPCWSTR sourceString
        )
    {
        VariantClear(this);
        V_VT(this) = VT_BSTR;
        V_BSTR(this) = SysAllocString(sourceString);
        if (!V_BSTR(this))
        {
            return E_OUTOFMEMORY;
        }

        return S_OK;
    }

    void
        SetObjectValue(
            IUnknown *sourceObject
        )
    {
        VariantClear(this);

        V_VT(this) = VT_UNKNOWN;
        V_UNKNOWN(this) = sourceObject;

        if (V_UNKNOWN(this))
        {
            V_UNKNOWN(this)->AddRef();
        }
    }

};


int wmain(int argc, wchar_t* argv[])
{
    if (argc != 2)
    {
        wprintf(L"Usage: ExtractComments.exe <filename>\n");
        exit(0);
    }
    wprintf(L"Starting.\n");
    LPCWSTR pFileName = argv[1];
    HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);

    if (SUCCEEDED(hr))
    {
        IOpcPackage * package = NULL;
        IOpcPart * documentPart = NULL;
        IOpcFactory * factory = NULL;
        hr = CoCreateInstance(
            __uuidof(OpcFactory),
            NULL,
            CLSCTX_INPROC_SERVER,
            __uuidof(IOpcFactory),
            (LPVOID*)&factory
        );
        if (SUCCEEDED(hr))
        {
            wprintf(L"Created factory.\n");
            hr = ::LoadPackage(factory, pFileName, &package);
            // See command arguments in project properties for specification of file to read.
        }
        if (SUCCEEDED(hr))
        {
            wprintf(L"Loaded package.\n");
            hr = ::FindDocumentInPackage(package, &documentPart);

        }
        IOpcPart *commentsPart = 0;
        if (SUCCEEDED(hr))
        {
            wprintf(L"Found document in package.\n");
            hr = ::FindCommentsInPackage(package, documentPart, &commentsPart);
        }
        if (SUCCEEDED(hr))
        {
            wprintf(L"Found comments in package.\n");
            hr = ::PrintCoreProperties(package);
        }
        if (SUCCEEDED(hr))
        {
            wprintf(L"Found core properties in package.\n");
            hr = ::PrintComments(commentsPart);
        }
        if (SUCCEEDED(hr))
        {
            wprintf(L"Found comments in package.\n");
        }

        // Release resources
        if (package)
        {
            package->Release();
            package = NULL;
        }

        if (documentPart)
        {
            documentPart->Release();
            documentPart = NULL;
        }

        if (factory)
        {
            factory->Release();
            factory = NULL;
        }
        CoUninitialize();
    }
    return 0;
}

HRESULT LoadPackage(
    IOpcFactory *factory,
    LPCWSTR packageName,
    IOpcPackage **outPackage)
{
    IStream * sourceFileStream = NULL;
    HRESULT hr = factory->CreateStreamOnFile(
        packageName,
        OPC_STREAM_IO_READ,
        NULL,
        0,
        &sourceFileStream);
    if (SUCCEEDED(hr))
    {
        hr = factory->ReadPackageFromStream(
            sourceFileStream,
            OPC_CACHE_ON_ACCESS,
            outPackage);
    }
    if (sourceFileStream)
    {
        sourceFileStream->Release();
        sourceFileStream = NULL;
    }
    return hr;
}
HRESULT FindDocumentInPackage(
    IOpcPackage *package,
    IOpcPart   **documentPart)
{
    return ::FindPartByRelationshipType(
        package,
        NULL,
        g_officeDocumentRelationshipType,
        g_wordProcessingContentType,
        documentPart);

}
HRESULT FindCommentsInPackage(
    IOpcPackage *package,
    IOpcPart   *documentPart,
    IOpcPart   **commentsPart)
{
    return ::FindPartByRelationshipType(
        package,
        documentPart,
        g_commentsRelationshipType,
        g_commentsContentType,
        commentsPart);

}
HRESULT FindCorePropertiesPart(
    IOpcPackage * package,
    IOpcPart **part)
{
    return ::FindPartByRelationshipType(
        package,
        NULL,
        g_corePropertiesRelationshipType,
        g_corePropertiesContentType,
        part);
}
HRESULT FindPartByRelationshipType(
    IOpcPackage *package,
    IOpcPart *parentPart,
    LPCWSTR relationshipType,
    LPCWSTR contentType,
    IOpcPart **part)
{
    *part = NULL;
    IOpcRelationshipSet * packageRels = NULL;
    IOpcRelationshipEnumerator * packageRelsEnum = NULL;
    IOpcPartSet * partSet = NULL;
    BOOL hasNext = false;

    HRESULT hr = package->GetPartSet(&partSet);

    if (SUCCEEDED(hr))
    {
        if (parentPart == NULL)
        {
            hr = package->GetRelationshipSet(&packageRels);
        }
        else
        {
            hr = parentPart->GetRelationshipSet(&packageRels);
        }
    }
    if (SUCCEEDED(hr))
    {
        hr = packageRels->GetEnumeratorForType(
            relationshipType,
            &packageRelsEnum);
    }
    if (SUCCEEDED(hr))
    {
        hr = packageRelsEnum->MoveNext(&hasNext);
    }
    while (SUCCEEDED(hr) && hasNext && *part == NULL)
    {
        IOpcPartUri * partUri = NULL;
        IOpcRelationship * currentRel = NULL;
        BOOL partExists = FALSE;

        hr = packageRelsEnum->GetCurrent(&currentRel);
        if (SUCCEEDED(hr))
        {
            hr = ::ResolveTargetUriToPart(currentRel, &partUri);
        }
        if (SUCCEEDED(hr))
        {
            hr = partSet->PartExists(partUri, &partExists);
        }
        if (SUCCEEDED(hr) && partExists)
        {
            LPWSTR currentContentType = NULL;
            IOpcPart * currentPart = NULL;
            hr = partSet->GetPart(partUri, &currentPart);
            IOpcPartUri * name = NULL;
            currentPart->GetName(&name);
            BSTR displayUri = NULL;
            name->GetDisplayUri(&displayUri);
            wprintf(L"currentPart: %s\n", displayUri);
            if (SUCCEEDED(hr) && contentType != NULL)
            {
                hr = currentPart->GetContentType(&currentContentType);
                wprintf(L"contentType: %s\n", currentContentType);
                if (SUCCEEDED(hr) && 0 == wcscmp(contentType, currentContentType))
                {
                    *part = currentPart;  // found what we are looking for
                    currentPart = NULL;
                }
            }
            if (SUCCEEDED(hr) && contentType == NULL)
            {
                *part = currentPart;
                currentPart = NULL;
            }
            CoTaskMemFree(static_cast<LPVOID>(currentContentType));
            if (currentPart)
            {
                currentPart->Release();
                currentPart = NULL;
            }
        }
        if (SUCCEEDED(hr))
        {
            hr = packageRelsEnum->MoveNext(&hasNext);
        }
        if (partUri)
        {
            partUri->Release();
            partUri = NULL;
        }

        if (currentRel)
        {
            currentRel->Release();
            currentRel = NULL;
        }
    }
    if (SUCCEEDED(hr) && *part == NULL)
    {
        // Loop complete without errors and no part found.
        hr = E_FAIL;
    }

    // Release resources
    if (packageRels)
    {
        packageRels->Release();
        packageRels = NULL;
    }

    if (packageRelsEnum)
    {
        packageRelsEnum->Release();
        packageRelsEnum = NULL;
    }

    if (partSet)
    {
        partSet->Release();
        partSet = NULL;
    }
    return hr;
}
HRESULT ResolveTargetUriToPart(
    IOpcRelationship *relationship,
    IOpcPartUri **resolvedUri
)
{
    IOpcUri * sourceUri = NULL;
    IUri * targetUri = NULL;
    OPC_URI_TARGET_MODE targetMode;
    HRESULT hr = relationship->GetTargetMode(&targetMode);
    if (SUCCEEDED(hr) && targetMode != OPC_URI_TARGET_MODE_INTERNAL)
    {
        return E_FAIL;
    }
    if (SUCCEEDED(hr))
    {
        hr = relationship->GetTargetUri(&targetUri);
    }
    if (SUCCEEDED(hr))
    {
        hr = relationship->GetSourceUri(&sourceUri);
    }
    if (SUCCEEDED(hr))
    {
        hr = sourceUri->CombinePartUri(targetUri, resolvedUri);
    }
    if (sourceUri)
    {
        sourceUri->Release();
        sourceUri = NULL;
    }
    if (targetUri)
    {
        targetUri->Release();
        targetUri = NULL;
    }
    return hr;
}
HRESULT PrintComments(
    IOpcPart *commentsPart)
{
    IXMLDOMDocument2 * commentsDom = NULL;

    HRESULT hr = ::DOMFromPart(
        commentsPart,
        g_commentsSelectionNamespaces,
        &commentsDom);
    if (SUCCEEDED(hr))
    {
        IXMLDOMNodeList * commentsNodeList = NULL;
        BSTR text = ::SysAllocString(L"//w:comment");
        hr = commentsDom->selectNodes(
            text,
            &commentsNodeList);
        if (SUCCEEDED(hr) && commentsNodeList != NULL)
        {
            // Iterate through comment nodes
            // http://msdn.microsoft.com/en-us/library/ms757073(VS.85).aspx
            long nodeListLength = 0;
            hr = commentsNodeList->get_length(&nodeListLength);

            for (int i = 0; i < nodeListLength; ++i)
            {
                IXMLDOMNode *item = NULL;
                hr = commentsNodeList->get_item(i, &item);
                SUCCEEDED(hr) ? 0 : throw hr;

                ::GetAttributesOfCommentNode(item);
                ::GetTextofCommentNode(item);
            }
        }
        // Release resources
        if (commentsNodeList)
        {
            commentsNodeList->Release();
            commentsNodeList = NULL;
        }
        SysFreeString(text);
    }
    // Release resources
    if (commentsPart)
    {
        commentsPart->Release();
        commentsPart = NULL;
    }

    if (commentsDom)
    {
        commentsDom->Release();
        commentsDom = NULL;
    }

    return hr;
}
HRESULT GetTextofCommentNode(
    IXMLDOMNode *node
)
{
    BSTR bstrQueryString1 = ::SysAllocString(L"w:p");
    BSTR bstrQueryString2 = ::SysAllocString(L"w:r");
    BSTR commentText = NULL;
    IXMLDOMNodeList *resultList1 = NULL;
    IXMLDOMNodeList *resultList2 = NULL;
    IXMLDOMNode *pNode = NULL, *rNode = NULL;

    long resultLength1, resultLength2;

    HRESULT hr = node->selectNodes(bstrQueryString1, &resultList1);
    SUCCEEDED(hr) ? 0 : throw hr;
    hr = resultList1->get_length(&resultLength1);
    if (SUCCEEDED(hr))
    {
        resultList1->reset();
        for (int i = 0; i < resultLength1; ++i)
        {
            resultList1->get_item(i, &pNode);
            if (pNode)
            {
                //wprintf(L"--Found a w:p node.\n");
                wprintf(L"\n");
                pNode->selectNodes(bstrQueryString2, &resultList2);
                SUCCEEDED(hr) ? 0 : throw hr;
                hr = resultList2->get_length(&resultLength2);
                if (SUCCEEDED(hr))
                {
                    resultList2->reset();
                    for (int j = 0; j < resultLength2; ++j)
                    {
                        resultList2->get_item(j, &rNode);
                        if (rNode)
                        {
                            rNode->get_text(&commentText);
                            //wprintf(L"----Found a w:r node. \n");
                            wprintf(commentText);
                        }
                    }
                }

            }
        }
    }

    ::SysFreeString(bstrQueryString1);  ::SysFreeString(bstrQueryString2);
    bstrQueryString1 = NULL;            bstrQueryString2 = NULL;
    resultList1->Release();    resultList2->Release();
    resultList1 = NULL;     resultList2 = NULL;
    pNode->Release();     rNode->Release();
    pNode = NULL;      rNode = NULL;
    return hr;
}
HRESULT GetAttributesOfCommentNode(
    IXMLDOMNode *node
)
{
    VARIANT commentAuthorStr, commentDateStr;
    BSTR bstrAttributeAuthor = ::SysAllocString(L"w:author");
    BSTR bstrAttributeDate = ::SysAllocString(L"w:date");

    // Get author and date attribute of the item.
    //http://msdn.microsoft.com/en-us/library/ms767592(VS.85).aspx
    IXMLDOMNamedNodeMap *attribs = NULL;
    IXMLDOMNode *AttrNode = NULL;
    HRESULT hr = node->get_attributes(&attribs);
    if (SUCCEEDED(hr) && attribs)
    {
        attribs->getNamedItem(bstrAttributeAuthor, &AttrNode);
        if (SUCCEEDED(hr) && AttrNode)
        {
            AttrNode->get_nodeValue(&commentAuthorStr);
        }
        AttrNode->Release();
        AttrNode = NULL;
        attribs->getNamedItem(bstrAttributeDate, &AttrNode);
        if (SUCCEEDED(hr) && AttrNode)
        {
            AttrNode->get_nodeValue(&commentDateStr);
        }
        AttrNode->Release();
        AttrNode = NULL;
    }
    attribs->Release();
    attribs = NULL;

    wprintf(L"\n-------------------------------------------------");
    wprintf(L"\nComment::\nAuthor: %s, Date: %s\n", commentAuthorStr.bstrVal, commentDateStr.bstrVal);

    ::SysFreeString(bstrAttributeAuthor); ::SysFreeString(bstrAttributeDate);
    bstrAttributeAuthor = NULL;    bstrAttributeDate = NULL;

    return hr;
}
HRESULT PrintCoreProperties(
    IOpcPackage *package)
{
    IOpcPart * corePropertiesPart = NULL;
    IXMLDOMDocument2 * corePropertiesDom = NULL;

    HRESULT hr = ::FindCorePropertiesPart(
        package,
        &corePropertiesPart);
    if (SUCCEEDED(hr))
    {
        hr = ::DOMFromPart(
            corePropertiesPart,
            g_corePropertiesSelectionNamespaces,
            &corePropertiesDom);
    }
    if (SUCCEEDED(hr))
    {
        IXMLDOMNode * creatorNode = NULL;
        BSTR text = ::SysAllocString(L"//dc:creator");
        hr = corePropertiesDom->selectSingleNode(
            text,
            &creatorNode);
        if (SUCCEEDED(hr) && creatorNode != NULL)
        {
            hr = creatorNode->get_text(&text);
        }
        if (SUCCEEDED(hr))
        {
            wprintf(L"Author: %s\n", (text != NULL) ? text : L"[missing author info]");
        }
        // Release resources
        if (creatorNode)
        {
            creatorNode->Release();
            creatorNode = NULL;
        }

        SysFreeString(text);

        // put other code here to read other properties
    }
    // Release resources
    if (corePropertiesPart)
    {
        corePropertiesPart->Release();
        corePropertiesPart = NULL;
    }

    if (corePropertiesDom)
    {
        corePropertiesDom->Release();
        corePropertiesDom = NULL;
    }
    return hr;
}

HRESULT DOMFromPart(
    IOpcPart * part,
    LPCWSTR selectionNamespaces,
    IXMLDOMDocument2 **document)
{
    IXMLDOMDocument2 * partContentXmlDocument = NULL;
    IStream * partContentStream = NULL;

    HRESULT hr = CoCreateInstance(
        __uuidof(DOMDocument60),
        NULL,
        CLSCTX_INPROC_SERVER,
        __uuidof(IXMLDOMDocument2),
        (LPVOID*)&partContentXmlDocument);
    if (SUCCEEDED(hr) && selectionNamespaces)
    {
        AutoVariant v;
        BSTR val1 = ::SysAllocString(L"XPath");
        BSTR val2 = ::SysAllocString(L"SelectionLanguage");
        BSTR val3 = ::SysAllocString(L"SelectionNamespaces");
        hr = v.SetBSTRValue(val1);
        if (SUCCEEDED(hr))
        {
            hr = partContentXmlDocument->setProperty(val2, v);
        }
        if (SUCCEEDED(hr))
        {
            AutoVariant v;
            hr = v.SetBSTRValue(selectionNamespaces);
            if (SUCCEEDED(hr))
            {
                hr = partContentXmlDocument->setProperty(val3, v);
            }
        }
        SysFreeString(val1);
        SysFreeString(val2);
        SysFreeString(val3);
    }
    if (SUCCEEDED(hr))
    {
        hr = part->GetContentStream(&partContentStream);
    }
    if (SUCCEEDED(hr))
    {
        VARIANT_BOOL isSuccessful = VARIANT_FALSE;
        AutoVariant vStream;
        vStream.SetObjectValue(partContentStream);
        hr = partContentXmlDocument->load(vStream, &isSuccessful);
        if (SUCCEEDED(hr) && isSuccessful == VARIANT_FALSE)
        {
            hr = E_FAIL;
        }
    }
    if (SUCCEEDED(hr))
    {
        *document = partContentXmlDocument;
        partContentXmlDocument = NULL;
    }
    // Release resources
    if (partContentXmlDocument)
    {
        partContentXmlDocument->Release();
        partContentXmlDocument = NULL;
    }

    if (partContentStream)
    {
        partContentStream->Release();
        partContentStream = NULL;
    }
    return hr;
} 