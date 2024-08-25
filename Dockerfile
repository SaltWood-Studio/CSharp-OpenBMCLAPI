# 使用微软提供的.NET SDK镜像作为构建阶段的基础镜像
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 设置工作目录
WORKDIR /app

# 复制项目文件到工作目录
COPY . .

# 恢复项目依赖项
RUN dotnet restore

# 编译项目
RUN dotnet build --configuration Release --output /app/build

# 发布项目
RUN dotnet publish --configuration Release --output /app/publish

# 使用Ubuntu镜像作为运行阶段的基础镜像
FROM ubuntu:24.04

# 安装.NET运行时依赖项
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y aspnetcore-runtime-8.0 && \
    rm -rf /var/lib/apt/lists/*

# 设置工作目录
WORKDIR /app

# 从构建阶段复制发布的输出文件到运行阶段的工作目录
COPY --from=build /app/publish .

# 运行CSharp-OpenBMCLAPI项目
ENTRYPOINT ["dotnet", "CSharp-OpenBMCLAPI.dll"]