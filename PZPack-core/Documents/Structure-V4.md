﻿## PZPK文件结构描述

## Version 1

#### 整体结构
+ 文件头 - 固定 68 Bytes
+ 信息区 - (n) Bytes
+ 索引区偏移量 - 固定 8 Bytes `Int64`
+ 内容区 - (n) Bytes
+ 索引区 - (n) Bytes

#### 文件头
+ 04 [int32] 文件版本号
+ 32 [bytes] 标识校验Hash
+ 32 [bytes] 密码校验Hash

#### 信息区
+ 04 [int32] 信息区内容长度
> 读取内容解密后
+ 04 [int32] 描述文本长度 = x
+ 04 [int32] 描述缩略图长度 = y
+ x [bytes] 描述文本 `UTF8`
+ y [bytes] 描述缩略图

#### 索引区偏移量
+ 08 [int64] 索引区偏移量(从0开始计算)

#### 内容区
> 基于索引区取得的文件信息读取

#### 索引区
> 从索引区偏移量读取至文件末尾，解密后
+ 04 [int32] 文件夹区内容长度 = x
+ 04 [int32] 文件区内容长度 = y
+ x [bytes] 文件夹区内容
+ y [bytes] 文件区内容

#### 索引区 - 文件夹区
> 按顺序读取
+ 04 [int32] 当前文件夹内容长度 = x
+ 04 [int32] 文件夹ID
+ 04 [int32] 父文件夹ID
+ (x - 8) [bytes] 文件夹名 `UTF8`

#### 索引区 - 文件区
> 按顺序读取
+ 04 [int32] 当前文件内容长度 = x
+ 04 [int32] 父文件夹ID
+ 08 [int64] 文件数据在pzpack文件内的偏移量(从0开始计算)
+ 04 [int32] 文件大小
+ (x - 16) [bytes] 文件名 `UTF8`

## Version 2
> 变更
#### 文件区
文件大小 size 由`Int32`(4Bytes)修改为`Int64`(8Bytes)  

## Version 4
> 变更
每一个加密块前添加16Bytes的随机向量(IV)，对应块的内容解密使用此向量