#!/usr/bin/env python3
"""Run the same JS suite with macOS's built-in JavaScriptCore. No packages needed."""
import ctypes as c
from pathlib import Path
lib=c.CDLL('/System/Library/Frameworks/JavaScriptCore.framework/JavaScriptCore')
ptr=c.c_void_p
lib.JSGlobalContextCreate.argtypes=[ptr];lib.JSGlobalContextCreate.restype=ptr
lib.JSStringCreateWithUTF8CString.argtypes=[c.c_char_p];lib.JSStringCreateWithUTF8CString.restype=ptr
lib.JSEvaluateScript.argtypes=[ptr,ptr,ptr,ptr,c.c_int,c.POINTER(ptr)];lib.JSEvaluateScript.restype=ptr
lib.JSValueToStringCopy.argtypes=[ptr,ptr,c.POINTER(ptr)];lib.JSValueToStringCopy.restype=ptr
lib.JSStringGetMaximumUTF8CStringSize.argtypes=[ptr];lib.JSStringGetMaximumUTF8CStringSize.restype=c.c_size_t
lib.JSStringGetUTF8CString.argtypes=[ptr,c.c_char_p,c.c_size_t];lib.JSStringGetUTF8CString.restype=c.c_size_t
lib.JSStringRelease.argtypes=[ptr]
lib.JSGlobalContextRelease.argtypes=[ptr]
ctx=lib.JSGlobalContextCreate(None)
def evaluate(code):
    script=lib.JSStringCreateWithUTF8CString(code.encode());error=ptr()
    result=lib.JSEvaluateScript(ctx,script,None,None,1,c.byref(error));lib.JSStringRelease(script)
    if error.value: result=error.value
    string=lib.JSValueToStringCopy(ctx,result,None);n=lib.JSStringGetMaximumUTF8CStringSize(string);buf=c.create_string_buffer(n)
    lib.JSStringGetUTF8CString(string,buf,n);lib.JSStringRelease(string)
    text=buf.value.decode()
    if error.value: raise RuntimeError(text)
    return text
base=Path(__file__).resolve().parent
try:
    evaluate('var logs=[];var console={log:(...args)=>logs.push(args.join(" "))};')
    evaluate((base.parent/'src/core.js').read_text())
    evaluate((base/'core.test.js').read_text())
    failures=int(evaluate('testResult.failed'))
    evaluate((base.parent/'src/prediction.js').read_text())
    evaluate((base/'prediction.test.js').read_text())
    failures+=int(evaluate('predictionResultTests.failed'))
    evaluate((base/'ui-harness.js').read_text())
    evaluate((base.parent/'src/locations.js').read_text())
    evaluate((base.parent/'src/app.js').read_text())
    evaluate((base/'overlays.test.js').read_text())
    failures+=int(evaluate('overlayResult.failed'))
    evaluate((base/'direct-harness.js').read_text())
    evaluate((base.parent/'src/direct-file.js').read_text())
    evaluate((base/'direct.test.js').read_text())
    failures+=int(evaluate('directResult.failed'))
    if int(evaluate('directResult.passed + directResult.failed'))!=8:raise RuntimeError('Async tests did not finish')
    evaluate('var previewJobs=new Map(),previewJobId=0;setTimeout=fn=>{previewJobs.set(++previewJobId,fn);return previewJobId;};var clearTimeout=id=>previewJobs.delete(id);')
    evaluate((base.parent/'src/prediction-ui.js').read_text())
    evaluate((base/'prediction-ui.test.js').read_text())
    failures+=int(evaluate('previewUiResult.failed'))
    print(evaluate('logs.join("\\n")'))
finally:lib.JSGlobalContextRelease(ctx)
raise SystemExit(bool(failures))
