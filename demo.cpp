#include "rxregsvc.h"
#include "aced.h"
#include "acdb.h"
#include "acut.h"
#include "tchar.h"
#include "adscodes.h"
#include <ctime>
#include <chrono>

// 初始化命令
static void InitPlugin();
// 清理资源
static void UnloadPlugin();

// 测试命令：测量打开DWG文件的耗时
static void TestOpenDwgTime();

// ============================================================================
// ObjectARX 入口点
// ============================================================================

extern "C" AcRx::AppRetCode acrxEntryPoint(AcRx::AppMsgCode msg, void* pkt)
{
    switch (msg)
    {
    case AcRx::kInitAppMsg:
        acrxDynamicLinker->unlockApplication(pkt);
        acrxRegisterAppMDIAware(pkt);
        InitPlugin();
        break;

    case AcRx::kUnloadAppMsg:
        UnloadPlugin();
        break;

    default:
        break;
    }

    return AcRx::kRetOK;
}

// ============================================================================
// 插件初始化
// ============================================================================

static void InitPlugin()
{
    // 注册命令
    acedRegCmds->addCommand(
        _T("MYPLUGIN_COMMANDS"),   // 命令组名
        _T("TestOpenDwg"),         // 全局命令名
        _T("TESTOPENDWG"),         // 本地命令名
        ACRX_CMD_TRANSPARENT,      // 命令标志
        TestOpenDwgTime            // 命令函数指针
    );

    acutPrintf(_T("\n[MyPlugin] 插件加载成功！输入 TESTOPENDWG 开始测试。"));
}

// ============================================================================
// 插件卸载
// ============================================================================

static void UnloadPlugin()
{
    // 注销命令组
    acedRegCmds->removeGroup(_T("MYPLUGIN_COMMANDS"));
    acutPrintf(_T("\n[MyPlugin] 插件已卸载。"));
}

// ============================================================================
// 命令实现：测试打开DWG文件耗时
// ============================================================================

static void TestOpenDwgTime()
{
    // 提示用户选择DWG文件
    struct resbuf* pFileName = nullptr;
    int rc = acedGetFileNavDialog(
        _T("选择DWG文件"),
        nullptr,
        _T("dwg"),
        _T("TestOpenDwgHistory"),
        1,
        &pFileName
    );

    if (rc != RTNORM || pFileName == nullptr || pFileName->resval.rstring == nullptr)
    {
        acutPrintf(_T("\n已取消操作。"));
        if (pFileName) acutRelRb(pFileName);
        return;
    }

    const ACHAR* dwgPath = pFileName->resval.rstring;

    // 检查文件是否存在
    FILE* testFile = nullptr;
    if (_wfopen_s(&testFile, dwgPath, L"r") != 0)
    {
        acutPrintf(_T("\n错误：文件不存在 - %s"), dwgPath);
        acutRelRb(pFileName);
        return;
    }
    fclose(testFile);

    acutPrintf(_T("\n正在测试打开文件: %s"), dwgPath);

    // 使用高精度计时器
    auto startTime = std::chrono::high_resolution_clock::now();

    // 创建数据库对象并读取文件
    AcDbDatabase* pDb = new AcDbDatabase(Adesk::kFalse);
    Acad::ErrorStatus es = pDb->readDwgFile(dwgPath);

    auto endTime = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(endTime - startTime);

    if (es == Acad::eOk)
    {
        acutPrintf(_T("\n========================================"));
        acutPrintf(_T("\n文件打开成功！"));
        acutPrintf(_T("\n耗时: %lld 毫秒"), duration.count());
        acutPrintf(_T("\n========================================"));

        delete pDb;
    }
    else
    {
        acutPrintf(_T("\n错误：无法打开文件，错误代码: %d"), es);
        delete pDb;
    }

    acutRelRb(pFileName);
}
