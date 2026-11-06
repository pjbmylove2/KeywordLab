# 키워드 랩 (KeywordLab) — AI 작업 규칙

Codex 등 AGENTS.md를 읽는 도구용. 내용은 CLAUDE.md와 같다. 규칙을 고칠 때는 두 파일을 함께 고친다.

## 프로젝트 한 줄 요약

티스토리·네이버 이슈형 블로그용으로 **지금 뜨는 키워드, 검색량, 최근 트렌드를 보고 주제와 추천 제목을 정하는 로컬 웹앱**이다.
본문 작성은 이 앱의 범위가 아니다 (Codex의 네이버 v1.0 / 티스토리 v2.3 마스터 프롬프트가 담당).

## 환경

- Visual Studio 2022, .NET 8 (`net8.0`), C# 12. 로컬 PC에서만 실행한다.
- ASP.NET Core Razor Pages. 프론트엔드 빌드 도구(npm, Tailwind 등)는 쓰지 않는다.
- 저장소는 **공개(public)** 다.

## 절대 규칙

1. **API 키·비밀번호·토큰을 코드, appsettings.json, 테스트, 커밋 메시지에 절대 쓰지 않는다.**
   키는 VS "사용자 비밀 관리"(secrets.json)에서만 읽는다. appsettings.json에는 빈 값의 키 이름만 둔다.
2. Core의 로직(후보 생성, 점수, 판단 기준)을 바꾸면 `tests/KeywordLab.Tests`의 테스트도 같이 고치거나 추가한다.
   `dotnet test`가 통과하지 않으면 작업 완료가 아니다.
3. 실제 네이버·구글 API를 호출하는 테스트는 만들지 않는다. `HttpMessageHandler` 가짜나 `Fakes/`의 가짜 제공자를 쓴다.
4. UI 색상·글꼴은 `src/KeywordLab.Web/wwwroot/css/site.css`의 CSS 변수만 쓴다. 새 색상을 하드코딩하지 않는다.
   (K-프로그래밍 블로그 다크 스킨 기준 색상)
5. 화면 문구는 한국어. 오류 문구는 무엇이 잘못됐고 어떻게 고치는지 말한다.
6. 한 번에 한 단계(docs/ROADMAP.md)만 작업한다. 범위 밖 리팩터링은 하지 않는다.
7. 작업 전 `docs/DECISIONS.md`를 읽는다.

## 구조

```
src/KeywordLab.Core            순수 로직. 외부 의존성 없음.
  Models/                      VolumeCount, KeywordVolume, BlogSearchSummary
  Text/                        KeywordNormalizer, CandidateGenerator (조사 제거 + 단어 조합)
  Assessment/                  CompetitionAssessor, AssessmentRules (경쟁도 기준값)
  Abstractions/                IKeywordVolumeProvider, IBlogSearchProvider, ITitleGenerator(교체 가능), IHandoffLog, ITrendFeed, IAutocompleteProvider, ITrendStore
  Titles/                      TemplateTitleGenerator, TitleScorer(플랫폼별 규칙 점수), IntentKeywordExtractor, CodexBlockBuilder
  Trends/                      IssueKeywordScorer(자동 모드 점수), CategoryMatcher(블로그별 허용 카테고리 단어 판별)
  Services/                    KeywordLookupService (키워드 직접 조회), TitleWorkshopService (주제·제목 확정), IssueKeywordService (오늘의 이슈 키워드)
src/KeywordLab.Infrastructure  외부 API 연동
  Naver/                       검색광고 API(HMAC 서명), 블로그 검색 API, 응답 파서
  Trends/                      GoogleTrendsRssClient, NaverAutocompleteClient(비공식, 실패 시 빈 목록)
  Storage/                     SqliteHandoffLog, SqliteTrendStore (넘긴 기록·수집한 트렌드, %LOCALAPPDATA%\KeywordLab\keywordlab.db)
src/KeywordLab.Web             Razor Pages 화면
tests/KeywordLab.Tests         xUnit
```

의존 방향: Web → Infrastructure → Core. Core는 아무것도 참조하지 않는다.

## 네이버 API 메모

- 검색광고 `/keywordstool`: 힌트 키워드는 **공백을 빼고** 한 번에 최대 5개. 응답 키워드도 공백이 없다.
  검색 수가 10 미만이면 숫자 대신 `"< 10"` 문자열이 온다 (`VolumeCount.IsUnderTen`).
  서명: `Base64(HMAC-SHA256(SecretKey, "{timestampMs}.GET./keywordstool"))`, 경로에 쿼리 문자열 제외.
- 블로그 검색은 NAVER API HUB: `GET https://naverapihub.apigw.ntruss.com/search/v1/blog`,
  헤더 `X-NCP-APIGW-API-KEY-ID`(Client ID) / `X-NCP-APIGW-API-KEY`(Client Secret). 개발자센터 키·`openapi.naver.com`은 쓰지 않는다.
  `total`이 경쟁 문서 수. 제목에 `<b>` 태그가 섞여 온다.
- 검색광고 API는 API HUB 대상이 아니다. 기존 HMAC 방식(키 3개) 그대로.
- 호출 한도가 있으므로 블로그 검색은 검색량 상위 후보 일부만 호출한다 (`KeywordLookupService.MaxBlogLookups`).
