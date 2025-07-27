导出的unitypackage文件中，为了能够正常在别的unity工程中复现，我把模型fbx的materials Reset了，否则在其他unity工程中打开会有材质显示错误，然后正常使用插件就能看到正确的材质了

![image-20250728044216570](C:\Users\93415\AppData\Roaming\Typora\typora-user-images\image-20250728044216570.png)

插件使用很直观，需要注意的点我用HelpBox文字描述强调显示了，正常一步步点就行，需要注意的是目录结构最好和下面相似：

# 项目目录结构

```
Assets/
├── c1020.unity
├── c1020.unity.meta
├── JiangYinuo/
│   ├── Character/
│   │   ├── Character.meta
│   │   ├── Sekiro/
│   │   │   ├── Sekiro.meta
│   │   │   ├── c1020/
│   │   │   │   ├── c1020.meta
│   │   │   │   ├── Fbx/
│   │   │   │   │   ├── Fbx.meta
│   │   │   │   │   ├── Animation/
│   │   │   │   │   │   ├── Animation.meta
│   │   │   │   │   │   ├── Prefab/
│   │   │   │   │   │   │   ├── ...（大量prefab及meta文件）
│   │   │   │   │   │   ├── Fbx/
│   │   │   │   │   │   │   ├── ...（大量fbx.meta文件）
│   │   │   │   │   ├── Model/
│   │   │   │   │   │   ├── Model.meta
│   │   │   │   │   │   ├── Materials/
│   │   │   │   │   │   │   ├── ...（材质mat及meta文件）
│   │   │   │   │   │   ├── Textures/
│   │   │   │   │   │   │   ├── ...（贴图PNG及meta文件）
│   │   │   │   │   │   ├── c1020.fbx
│   │   │   │   │   │   ├── c1020.fbx.meta
│   ├── Editor/
│   │   ├── Editor.meta
│   │   ├── Model_Anim_Fbx_ImportTool.cs
│   │   ├── Model_Anim_Fbx_ImportTool.cs.meta
│   │   ├── Anim_PreviewTool.cs
│   │   ├── Anim_PreviewTool.cs.meta
├── JiangYinuo.meta
```

> 说明：
> - 省略了部分大量重复的prefab、fbx、材质、贴图等文件，仅以“...（文件类型及meta文件）”表示。
> - 该结构基于当前Assets目录下的实际文件和文件夹递归扫描结果。 