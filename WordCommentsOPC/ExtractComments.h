#include "msopc.h"
#include "msxml6.h"
#include "pch.h"

HRESULT LoadPackage(IOpcFactory *factory, LPCWSTR packageName, IOpcPackage **outPackage);
HRESULT FindDocumentInPackage(IOpcPackage *package, IOpcPart  **documentPart);
HRESULT FindCommentsInPackage(IOpcPackage *package, IOpcPart  *parentPart, IOpcPart  **documentPart);
HRESULT FindPartByRelationshipType(IOpcPackage *package, IOpcPart *parentPart, LPCWSTR relationshipType, LPCWSTR contentType, IOpcPart **part);
HRESULT ResolveTargetUriToPart(IOpcRelationship *relativeUri, IOpcPartUri **resolvedUri);
HRESULT PrintCoreProperties(IOpcPackage *package);
HRESULT PrintComments(IOpcPart *part);
HRESULT GetAttributesOfCommentNode(IXMLDOMNode *node);
HRESULT GetTextofCommentNode(IXMLDOMNode *node);
HRESULT FindCorePropertiesPart(IOpcPackage *package, IOpcPart **part);
HRESULT DOMFromPart(IOpcPart *part, LPCWSTR selectionNamespaces, IXMLDOMDocument2 **document);

static const WCHAR g_officeDocumentRelationshipType[] =
L"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
static const WCHAR g_wordProcessingContentType[] =
L"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
static const WCHAR g_corePropertiesRelationshipType[] =
L"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
static const WCHAR g_corePropertiesContentType[] =
L"application/vnd.openxmlformats-package.core-properties+xml";
static const WCHAR g_commentsRelationshipType[] =
L"http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments";
static const WCHAR g_commentsContentType[] =
L"application/vnd.openxmlformats-officedocument.wordprocessingml.comments+xml";
static const WCHAR g_corePropertiesSelectionNamespaces[] =
L"xmlns:cp='http://schemas.openxmlformats.org/package/2006/metadata/core-properties' "
L"xmlns:dc='http://purl.org/dc/elements/1.1/' "
L"xmlns:dcterms='http://purl.org/dc/terms/' "
L"xmlns:dcmitype='http://purl.org/dc/dcmitype/' "
L"xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'";
static const WCHAR g_commentsSelectionNamespaces[] =
L"xmlns:wpc='http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas' "
L"xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' "
L"xmlns:o='urn:schemas-microsoft-com:office:office' "
L"xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' "
L"xmlns:m='http://schemas.openxmlformats.org/officeDocument/2006/math' "
L"xmlns:v='urn:schemas-microsoft-com:vml' "
L"xmlns:wp14='http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing' "
L"xmlns:wp='http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing' "
L"xmlns:w10='urn:schemas-microsoft-com:office:word' "
L"xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main' "
L"xmlns:w14='http://schemas.microsoft.com/office/word/2010/wordml' "
L"xmlns:wpg='http://schemas.microsoft.com/office/word/2010/wordprocessingGroup' "
L"xmlns:wpi='http://schemas.microsoft.com/office/word/2010/wordprocessingInk' "
L"xmlns:wne='http://schemas.microsoft.com/office/word/2006/wordml' "
L"xmlns:wps='http://schemas.microsoft.com/office/word/2010/wordprocessingShape' ";