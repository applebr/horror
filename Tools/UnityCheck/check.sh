#!/usr/bin/env bash
# Unity 참조 어셈블리로 전체 스크립트를 컴파일해 본다 (Unity 설치 불필요).
# 6000.0.23 (Unity 6 LTS) 와 2021.3.45 (2021 LTS), 구/신 입력 설정 각각.
set -euo pipefail
cd "$(dirname "$0")"
command -v dotnet >/dev/null || { echo "dotnet SDK가 필요합니다"; exit 1; }
for v in 6000.0.23 2021.3.45; do
  if [ ! -d "refs/$v/lib/net45" ]; then
    mkdir -p "refs/$v"
    echo "== UnityEngine.Modules $v 받는 중"
    curl -sSL -o "refs/ue-$v.nupkg" "https://nuget.bepinex.dev/v3/package/unityengine.modules/$v/unityengine.modules.$v.nupkg"
    (cd "refs/$v" && unzip -q -o "../ue-$v.nupkg")
  fi
done
fail=0
for cfg in "6000.0.23 true" "6000.0.23 false" "2021.3.45 true"; do
  set -- $cfg
  echo "== Unity $1, LegacyInput=$2"
  if dotnet build -nologo -v q -p:UnityVersion=$1 -p:LegacyInput=$2 2>&1 | grep -E "error|warning CS|Build succeeded" | sort -u; then :; fi
  if ! dotnet build -nologo -v q -p:UnityVersion=$1 -p:LegacyInput=$2 >/dev/null 2>&1; then fail=1; fi
done
exit $fail
