## PZPK文件结构描述
Version = 1 版本
> 注: 1B = 1Byte

## 文件头
### 验证区 offset = 0
+ 4B 文件版本
+ 32B 应用签名散列
+ 32B 密码散列

### 信息区 offset = 68
+ 4B 信息区长度 infoLength
> 以下加密
+ 4B 描述文本长度a 0为不包含描述文本
+ 4B 预览图区长度b 0为不包含预览图
+ a*B 文本UTF8
+ b*B 图片

### 内容区 offset = 72 + infoLength
+ 8B 索引区偏移 indexOffset
> 以下按文件加密 依赖索引区信息

### 索引区 offset = indexOffset
> 本区加密
+ 4B 目录信息长度a
+ 4B 文件信息长度b
+ a*B 目录信息 * n
+ b*B 文件信息 * n

### 文件结尾

## 索引信息结构
### 文件夹
+ 4B 当前目录信息长度
+ 4B 目录ID
+ 4B 父目录ID
+ *B 文件夹名

### 文件
+ 4B 当前文件信息长度
+ 4B 文件夹ID
+ 8B 文件起始位置
+ 4B 文件长度
+ *B 文件名
