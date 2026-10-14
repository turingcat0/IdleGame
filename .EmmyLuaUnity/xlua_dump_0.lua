---@meta
---@class System.Collections.Generic.List<T>: object, System.Collections.Generic.IList<T>, System.Collections.Generic.ICollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IList, System.Collections.ICollection, System.Collections.IEnumerable
---@overload fun(): System.Collections.Generic.List
---@overload fun(collection: System.Collections.Generic.IEnumerable<T>): System.Collections.Generic.List
---@overload fun(capacity: integer): System.Collections.Generic.List
local List = {}
---@type integer
List.Capacity = nil

---@type integer
List.Count = nil

---@param item T
---@return void
function List:Add(item)
end

---@param collection System.Collections.Generic.IEnumerable<T>
---@return void
function List:AddRange(collection)
end

---@return System.Collections.ObjectModel.ReadOnlyCollection<T>
function List:AsReadOnly()
end

---@param index integer
---@param count integer
---@param item T
---@param comparer System.Collections.Generic.IComparer<T>
---@return integer
function List:BinarySearch(index, count, item, comparer)
end

---@param item T
---@return integer
function List:BinarySearch(item)
end

---@param item T
---@param comparer System.Collections.Generic.IComparer<T>
---@return integer
function List:BinarySearch(item, comparer)
end

---@return void
function List:Clear()
end

---@param item T
---@return boolean
function List:Contains(item)
end

---@param converter System.Converter<T, TOutput>
---@return TOutput[]
function List:ConvertAll(converter)
end

---@param index integer
---@param array T[]
---@param arrayIndex integer
---@param count integer
---@return void
function List:CopyTo(index, array, arrayIndex, count)
end

---@param array T[]
---@return void
function List:CopyTo(array)
end

---@param array T[]
---@param arrayIndex integer
---@return void
function List:CopyTo(array, arrayIndex)
end

---@param match System.Predicate<T>
---@return boolean
function List:Exists(match)
end

---@param match System.Predicate<T>
---@return T
function List:Find(match)
end

---@param match System.Predicate<T>
---@return T[]
function List:FindAll(match)
end

---@param startIndex integer
---@param count integer
---@param match System.Predicate<T>
---@return integer
function List:FindIndex(startIndex, count, match)
end

---@param startIndex integer
---@param match System.Predicate<T>
---@return integer
function List:FindIndex(startIndex, match)
end

---@param match System.Predicate<T>
---@return integer
function List:FindIndex(match)
end

---@param match System.Predicate<T>
---@return T
function List:FindLast(match)
end

---@param startIndex integer
---@param count integer
---@param match System.Predicate<T>
---@return integer
function List:FindLastIndex(startIndex, count, match)
end

---@param startIndex integer
---@param match System.Predicate<T>
---@return integer
function List:FindLastIndex(startIndex, match)
end

---@param match System.Predicate<T>
---@return integer
function List:FindLastIndex(match)
end

---@param action System.Action<T>
---@return void
function List:ForEach(action)
end

---@return T[]
function List:GetEnumerator()
end

---@param index integer
---@param count integer
---@return T[]
function List:GetRange(index, count)
end

---@param item T
---@return integer
function List:IndexOf(item)
end

---@param item T
---@param index integer
---@return integer
function List:IndexOf(item, index)
end

---@param item T
---@param index integer
---@param count integer
---@return integer
function List:IndexOf(item, index, count)
end

---@param index integer
---@param item T
---@return void
function List:Insert(index, item)
end

---@param index integer
---@param collection System.Collections.Generic.IEnumerable<T>
---@return void
function List:InsertRange(index, collection)
end

---@param item T
---@return integer
function List:LastIndexOf(item)
end

---@param item T
---@param index integer
---@return integer
function List:LastIndexOf(item, index)
end

---@param item T
---@param index integer
---@param count integer
---@return integer
function List:LastIndexOf(item, index, count)
end

---@param item T
---@return boolean
function List:Remove(item)
end

---@param match System.Predicate<T>
---@return integer
function List:RemoveAll(match)
end

---@param index integer
---@return void
function List:RemoveAt(index)
end

---@param index integer
---@param count integer
---@return void
function List:RemoveRange(index, count)
end

---@return void
function List:Reverse()
end

---@param index integer
---@param count integer
---@return void
function List:Reverse(index, count)
end

---@return void
function List:Sort()
end

---@param comparer System.Collections.Generic.IComparer<T>
---@return void
function List:Sort(comparer)
end

---@param comparison System.Comparison<T>
---@return void
function List:Sort(comparison)
end

---@param index integer
---@param count integer
---@param comparer System.Collections.Generic.IComparer<T>
---@return void
function List:Sort(index, count, comparer)
end

---@return T[]
function List:ToArray()
end

---@return void
function List:TrimExcess()
end

---@param match System.Predicate<T>
---@return boolean
function List:TrueForAll(match)
end


---@class System.Collections.Generic.Dictionary<TKey, TValue>: object, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IDictionary, System.Collections.ICollection, System.Collections.IEnumerable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
---@overload fun(): System.Collections.Generic.Dictionary
---@overload fun(dictionary: System.Collections.Generic.IDictionary<TKey, TValue>): System.Collections.Generic.Dictionary
---@overload fun(dictionary: System.Collections.Generic.IDictionary<TKey, TValue>, comparer: System.Collections.Generic.IEqualityComparer<TKey>): System.Collections.Generic.Dictionary
---@overload fun(collection: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>): System.Collections.Generic.Dictionary
---@overload fun(collection: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, comparer: System.Collections.Generic.IEqualityComparer<TKey>): System.Collections.Generic.Dictionary
---@overload fun(comparer: System.Collections.Generic.IEqualityComparer<TKey>): System.Collections.Generic.Dictionary
---@overload fun(capacity: integer): System.Collections.Generic.Dictionary
---@overload fun(capacity: integer, comparer: System.Collections.Generic.IEqualityComparer<TKey>): System.Collections.Generic.Dictionary
local Dictionary = {}
---@type System.Collections.Generic.IEqualityComparer<TKey>
Dictionary.Comparer = nil

---@type integer
Dictionary.Count = nil

---@type table
Dictionary.Keys = nil

---@type table
Dictionary.Values = nil

---@param key TKey
---@param value TValue
---@return void
function Dictionary:Add(key, value)
end

---@return void
function Dictionary:Clear()
end

---@param key TKey
---@return boolean
function Dictionary:ContainsKey(key)
end

---@param value TValue
---@return boolean
function Dictionary:ContainsValue(value)
end

---@param capacity integer
---@return integer
function Dictionary:EnsureCapacity(capacity)
end

---@return table
function Dictionary:GetEnumerator()
end

---@param info System.Runtime.Serialization.SerializationInfo
---@param context System.Runtime.Serialization.StreamingContext
---@return void
function Dictionary:GetObjectData(info, context)
end

---@param sender any
---@return void
function Dictionary:OnDeserialization(sender)
end

---@param key TKey
---@return boolean
function Dictionary:Remove(key)
end

---@param key TKey
---@return boolean, TValue
function Dictionary:Remove(key, value)
end

---@return void
function Dictionary:TrimExcess()
end

---@param capacity integer
---@return void
function Dictionary:TrimExcess(capacity)
end

---@param key TKey
---@param value TValue
---@return boolean
function Dictionary:TryAdd(key, value)
end

---@param key TKey
---@return boolean, TValue
function Dictionary:TryGetValue(key, value)
end


---@source "file:///d:/Workspace/xluaDemo/Assets/XLua/Src/LuaAssetDatabase.cs#8:13"
---@class LuaAssetDatabase: object
---@overload fun(): LuaAssetDatabase
local LuaAssetDatabase = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/XLua/Src/LuaAssetDatabase.cs#10:25"
---@param path string
---@param type System.Type
---@return UnityEngine.Object
function LuaAssetDatabase.LoadAssetAtPath(path, type)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/XLua/Src/LuaAssetDatabase.cs#19:27"
---@param path string
---@param type System.Type
---@return UnityEngine.Object[]
function LuaAssetDatabase.LoadAll(path, type)
end


---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#23:13"
---@class LuaBehaviour: UnityEngine.MonoBehaviour
---@overload fun(): LuaBehaviour
local LuaBehaviour = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#25:21"
---@type UnityEngine.TextAsset
LuaBehaviour.luaScript = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#26:23"
---@type Injection[]
LuaBehaviour.injections = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#37:20"
---@type XLua.LuaTable
LuaBehaviour.scriptEnv = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#39:18"
---@param filepath string
---@return byte[], string
function LuaBehaviour:MyLoader(filepath)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#48:17"
---@return void
function LuaBehaviour:luaEnvInit()
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#105:20"
---@param name string
---@return XLua.LuaTable
function LuaBehaviour:GetLuaObject(name)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/LuaBehaviour.cs#113:20"
---@return XLua.LuaTable
function LuaBehaviour:GetScriptEnv()
end


---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#7:13"
---@class SystemUtils: object
---@overload fun(): SystemUtils
local SystemUtils = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#9:27"
---@type System.Text.Encoding
SystemUtils.fileEncoding = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#12:25"
---@param path string
---@return string
function SystemUtils.ReadFileText(path)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#16:25"
---@param path string
---@return byte[]
function SystemUtils.ReadFileBytes(path)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#21:23"
---@param path string
---@param content string
---@return void
function SystemUtils.WriteFileByString(path, content)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#31:23"
---@param path string
---@param content byte[]
---@return void
function SystemUtils.WriteFileByBytes(path, content)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#41:25"
---@param path string
---@return string
function SystemUtils.ComputeFileMD5(path)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/SystemUtils.cs#46:25"
---@param bytes byte[]
---@return string
function SystemUtils.ComputeBytesMD5(bytes)
end


---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/EngineTest.cs#6:13"
---@class EngineTest: UnityEngine.MonoBehaviour
---@overload fun(): EngineTest
local EngineTest = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/EngineTest.cs#9:18"
---@type string
EngineTest.msg = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/Resources/Scripts/EngineTest.cs#26:16"
---@return void
function EngineTest:info()
end


---@class System.Security.Cryptography.MD5: System.Security.Cryptography.HashAlgorithm, System.Security.Cryptography.ICryptoTransform, System.IDisposable
---@overload fun(): System.Security.Cryptography.MD5
local MD5 = {}
---@return System.Security.Cryptography.MD5
function MD5.Create()
end

---@param algName string
---@return System.Security.Cryptography.MD5
function MD5.Create(algName)
end


---@class UnityEngine.Networking.UnityWebRequest: object, System.IDisposable
---@overload fun(): UnityEngine.Networking.UnityWebRequest
---@overload fun(url: string): UnityEngine.Networking.UnityWebRequest
---@overload fun(uri: System.Uri): UnityEngine.Networking.UnityWebRequest
---@overload fun(url: string, method: string): UnityEngine.Networking.UnityWebRequest
---@overload fun(uri: System.Uri, method: string): UnityEngine.Networking.UnityWebRequest
---@overload fun(url: string, method: string, downloadHandler: UnityEngine.Networking.DownloadHandler, uploadHandler: UnityEngine.Networking.UploadHandler): UnityEngine.Networking.UnityWebRequest
---@overload fun(uri: System.Uri, method: string, downloadHandler: UnityEngine.Networking.DownloadHandler, uploadHandler: UnityEngine.Networking.UploadHandler): UnityEngine.Networking.UnityWebRequest
local UnityWebRequest = {}
---@type string
UnityWebRequest.kHttpVerbGET = nil

---@type string
UnityWebRequest.kHttpVerbHEAD = nil

---@type string
UnityWebRequest.kHttpVerbPOST = nil

---@type string
UnityWebRequest.kHttpVerbPUT = nil

---@type string
UnityWebRequest.kHttpVerbCREATE = nil

---@type string
UnityWebRequest.kHttpVerbDELETE = nil

---@type boolean
UnityWebRequest.disposeCertificateHandlerOnDispose = nil

---@type boolean
UnityWebRequest.disposeDownloadHandlerOnDispose = nil

---@type boolean
UnityWebRequest.disposeUploadHandlerOnDispose = nil

---@type string
UnityWebRequest.method = nil

---@type string
UnityWebRequest.error = nil

---@type boolean
UnityWebRequest.useHttpContinue = nil

---@type string
UnityWebRequest.url = nil

---@type System.Uri
UnityWebRequest.uri = nil

---@type integer
UnityWebRequest.responseCode = nil

---@type number
UnityWebRequest.uploadProgress = nil

---@type boolean
UnityWebRequest.isModifiable = nil

---@type boolean
UnityWebRequest.isDone = nil

---@type boolean
UnityWebRequest.isNetworkError = nil

---@type boolean
UnityWebRequest.isHttpError = nil

---@type UnityEngine.Networking.UnityWebRequest.Result
UnityWebRequest.result = nil

---@type number
UnityWebRequest.downloadProgress = nil

---@type integer
UnityWebRequest.uploadedBytes = nil

---@type integer
UnityWebRequest.downloadedBytes = nil

---@type integer
UnityWebRequest.redirectLimit = nil

---@type boolean
UnityWebRequest.chunkedTransfer = nil

---@type UnityEngine.Networking.UploadHandler
UnityWebRequest.uploadHandler = nil

---@type UnityEngine.Networking.DownloadHandler
UnityWebRequest.downloadHandler = nil

---@type UnityEngine.Networking.CertificateHandler
UnityWebRequest.certificateHandler = nil

---@type integer
UnityWebRequest.timeout = nil

---@type boolean
UnityWebRequest.isError = nil

---@return void
function UnityWebRequest.ClearCookieCache()
end

---@param uri System.Uri
---@return void
function UnityWebRequest.ClearCookieCache(uri)
end

---@return void
function UnityWebRequest:Dispose()
end

---@return UnityEngine.AsyncOperation
function UnityWebRequest:Send()
end

---@return UnityEngine.Networking.UnityWebRequestAsyncOperation
function UnityWebRequest:SendWebRequest()
end

---@return void
function UnityWebRequest:Abort()
end

---@param name string
---@return string
function UnityWebRequest:GetRequestHeader(name)
end

---@param name string
---@param value string
---@return void
function UnityWebRequest:SetRequestHeader(name, value)
end

---@param name string
---@return string
function UnityWebRequest:GetResponseHeader(name)
end

---@return table
function UnityWebRequest:GetResponseHeaders()
end

---@param uri string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Get(uri)
end

---@param uri System.Uri
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Get(uri)
end

---@param uri string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Delete(uri)
end

---@param uri System.Uri
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Delete(uri)
end

---@param uri string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Head(uri)
end

---@param uri System.Uri
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Head(uri)
end

---@param uri string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetTexture(uri)
end

---@param uri string
---@param nonReadable boolean
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetTexture(uri, nonReadable)
end

---@param uri string
---@param audioType UnityEngine.AudioType
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetAudioClip(uri, audioType)
end

---@param uri string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetAssetBundle(uri)
end

---@param uri string
---@param crc integer
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetAssetBundle(uri, crc)
end

---@param uri string
---@param version integer
---@param crc integer
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetAssetBundle(uri, version, crc)
end

---@param uri string
---@param hash UnityEngine.Hash128
---@param crc integer
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetAssetBundle(uri, hash, crc)
end

---@param uri string
---@param cachedAssetBundle UnityEngine.CachedAssetBundle
---@param crc integer
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.GetAssetBundle(uri, cachedAssetBundle, crc)
end

---@param uri string
---@param bodyData byte[]
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Put(uri, bodyData)
end

---@param uri System.Uri
---@param bodyData byte[]
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Put(uri, bodyData)
end

---@param uri string
---@param bodyData string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Put(uri, bodyData)
end

---@param uri System.Uri
---@param bodyData string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Put(uri, bodyData)
end

---@param uri string
---@param postData string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, postData)
end

---@param uri System.Uri
---@param postData string
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, postData)
end

---@param uri string
---@param formData UnityEngine.WWWForm
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, formData)
end

---@param uri System.Uri
---@param formData UnityEngine.WWWForm
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, formData)
end

---@param uri string
---@param multipartFormSections UnityEngine.Networking.IMultipartFormSection[]
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, multipartFormSections)
end

---@param uri System.Uri
---@param multipartFormSections UnityEngine.Networking.IMultipartFormSection[]
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, multipartFormSections)
end

---@param uri string
---@param multipartFormSections UnityEngine.Networking.IMultipartFormSection[]
---@param boundary byte[]
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, multipartFormSections, boundary)
end

---@param uri System.Uri
---@param multipartFormSections UnityEngine.Networking.IMultipartFormSection[]
---@param boundary byte[]
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, multipartFormSections, boundary)
end

---@param uri string
---@param formFields table
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, formFields)
end

---@param uri System.Uri
---@param formFields table
---@return UnityEngine.Networking.UnityWebRequest
function UnityWebRequest.Post(uri, formFields)
end

---@param s string
---@return string
function UnityWebRequest.EscapeURL(s)
end

---@param s string
---@param e System.Text.Encoding
---@return string
function UnityWebRequest.EscapeURL(s, e)
end

---@param s string
---@return string
function UnityWebRequest.UnEscapeURL(s)
end

---@param s string
---@param e System.Text.Encoding
---@return string
function UnityWebRequest.UnEscapeURL(s, e)
end

---@param multipartFormSections UnityEngine.Networking.IMultipartFormSection[]
---@param boundary byte[]
---@return byte[]
function UnityWebRequest.SerializeFormSections(multipartFormSections, boundary)
end

---@return byte[]
function UnityWebRequest.GenerateBoundary()
end

---@param formFields table
---@return byte[]
function UnityWebRequest.SerializeSimpleForm(formFields)
end


---@class UnityEngine.Networking.DownloadHandler: object, System.IDisposable
---@overload fun(): UnityEngine.Networking.DownloadHandler
local DownloadHandler = {}
---@type boolean
DownloadHandler.isDone = nil

---@type string
DownloadHandler.error = nil

---@type Unity.Collections.NativeArray<byte>.ReadOnly
DownloadHandler.nativeData = nil

---@type byte[]
DownloadHandler.data = nil

---@type string
DownloadHandler.text = nil

---@return void
function DownloadHandler:Dispose()
end


---Dynamic Scroll View
---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#19:26"
---@class Mosframe.DynamicScrollView: UnityEngine.EventSystems.UIBehaviour
---@overload fun(): Mosframe.DynamicScrollView
local DynamicScrollView = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#21:28"
---@type integer
DynamicScrollView.totalItemCount = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#22:28"
---@type UnityEngine.RectTransform
DynamicScrollView.itemPrototype = nil

---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#24:20"
---@return void
function DynamicScrollView:scrollToLastPos()
end

---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#29:20"
---@param itemIndex integer
---@return void
function DynamicScrollView:scrollByItemIndex(itemIndex)
end

---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#36:20"
---@return void
function DynamicScrollView:refresh()
end

---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicScrollView.cs#214:28"
---@return void
function DynamicScrollView:init()
end


---Dynamic Horizontal Scroll View
---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicHScrollView.cs#15:17"
---@class Mosframe.DynamicHScrollView: Mosframe.DynamicScrollView
---@overload fun(): Mosframe.DynamicHScrollView
local DynamicHScrollView = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicHScrollView.cs#22:29"
---@return void
function DynamicHScrollView:init()
end


---Dynamic Vertical Scroll View
---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicVScrollView.cs#15:17"
---@class Mosframe.DynamicVScrollView: Mosframe.DynamicScrollView
---@overload fun(): Mosframe.DynamicVScrollView
local DynamicVScrollView = {}
---@source "file:///d:/Workspace/xluaDemo/Assets/3rd/Mosframe/ScrollView/DynamicVScrollView.cs#22:29"
---@return void
function DynamicVScrollView:init()
end


---@interface IDynamicScrollViewItem

