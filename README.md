# 키워드 랩 (KeywordLab)

이슈형 블로그(티스토리·네이버)를 위해 **지금 뜨는 키워드와 검색량, 최근 트렌드를 보고 주제와 추천 제목을 정하는** 로컬 웹앱입니다.
본문은 이 앱에서 정한 주제·제목을 Codex의 블로그 마스터 프롬프트에 넘겨 작성합니다.

현재 단계: **3단계 — 오늘의 이슈 키워드 + 설정** (구글 트렌드 RSS 수집 → 네이버 검색량·문서 수·자동완성 → 점수순 → 제목 정하기, 블로그별 허용 카테고리·금지 표현 설정)

## 준비물

- Visual Studio 2022 (워크로드: "ASP.NET 및 웹 개발")
- .NET 8 SDK
- 네이버 검색광고 API 키: searchad.naver.com 로그인 → 도구 → API 사용 관리 (CUSTOMER_ID, 엑세스라이선스, 비밀키)
- 네이버 검색 API 키: 네이버 클라우드 콘솔 → NAVER API HUB → Application 등록 → 사용 API에 "검색 - 블로그" 추가 (Client ID, Client Secret).
  개발자센터(developers.naver.com)에서 받은 키는 API HUB에서 쓸 수 없습니다.

## 사용자 비밀 설정 (키 넣기)

키는 저장소 밖에만 둡니다. 이 저장소는 공개 저장소입니다.

1. `KeywordLab.sln`을 VS2022로 엽니다.
2. 솔루션 탐색기에서 `KeywordLab.Web` 프로젝트를 우클릭 → **사용자 비밀 관리**.
3. 열린 `secrets.json`에 아래 형식으로 값을 넣고 저장합니다.

```json
{
  "Naver": {
    "SearchAd": {
      "CustomerId": "여기에",
      "AccessLicense": "여기에",
      "SecretKey": "여기에"
    },
    "OpenApi": {
      "ClientId": "여기에",
      "ClientSecret": "여기에"
    }
  }
}
```

## 실행

- 시작 프로젝트를 `KeywordLab.Web`으로 지정하고 F5.
- 첫 화면 **오늘의 이슈 키워드**는 열 때마다 구글 트렌드 RSS를 수집해 점수순으로 보여줍니다. 점수 기준값은 `appsettings.json`의 `IssueScoring`에서 고칩니다.
- 왼쪽 메뉴 **설정**에서 블로그별 허용 카테고리(한 줄에 `카테고리: 단어, 단어`)와 제목 금지 표현을 고칩니다. 값은 `%LOCALAPPDATA%\KeywordLab\settings.json`에 저장되어 GitHub에 올라가지 않습니다. 저장한 적이 없으면 `appsettings.json`의 `Blogs`·`Titles:BannedPhrases`를 기본값으로 씁니다.
- 왼쪽 메뉴 **키워드 직접 조회**에서 주제를 입력합니다. 예: `겨울철 전기요금 아끼는 법`
- 키워드 카드의 **제목 정하기**를 누르면 티스토리·네이버 제목 후보와 점수가 나옵니다. 추천 1개·대안 2개를 고르고 **블록 복사**를 누르면 Codex 입력 블록이 복사되고 기록이 `%LOCALAPPDATA%\KeywordLab\keywordlab.db`에 저장됩니다.

## 테스트

- VS 메뉴 **테스트 → 모든 테스트 실행**, 또는 터미널에서 `dotnet test`.
- 테스트는 실제 API를 호출하지 않습니다.

## GitHub에 올리기 (처음 한 번)

1. VS 메뉴 **Git → Git 리포지토리 만들기**.
2. "GitHub에 푸시"를 고르고 계정, 저장소 이름, **비공개 해제(공개)** 를 확인한 뒤 만들기.
3. GitHub 저장소 **Settings → Code security**에서 **Push protection**이 켜져 있는지 확인합니다. 키가 섞인 커밋을 막아 줍니다.

푸시하면 GitHub Actions가 빌드와 테스트를 자동으로 돌립니다 (`.github/workflows/build.yml`).

## 경쟁도 판단 기준 (임시값)

`src/KeywordLab.Core/Assessment/CompetitionAssessor.cs`의 `AssessmentRules`에서 조정합니다.

| 판단 | 기준 |
|---|---|
| 검색량 적음 | 월 검색 50회 미만 |
| 추천 · 진입 쉬움 | 검색 1회당 블로그 문서 0.5개 이하 |
| 추천 | 그 사이 |
| 경쟁 높음 | 검색 1회당 블로그 문서 3개 초과 |

실제 글 성과를 보면서 바꿔 나갈 값입니다.

## 로드맵

[docs/ROADMAP.md](docs/ROADMAP.md)
