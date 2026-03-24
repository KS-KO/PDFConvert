# PDF to Office/Slides Converter 기술설계서

## 1. 문서 개요

- 문서명: PDF to Office/Slides Converter 기술설계서
- 버전: v0.2
- 작성일: 2026-03-24
- 기준 문서: `PRD_PDF_Converter.md`

## 2. 기술 목표

- `.NET 9` 기반의 Windows 데스크톱 애플리케이션 구현
- `WPF + MVVM` 구조를 적용한 유지보수 가능한 UI 아키텍처 수립
- `x64` 전용 빌드로 대용량 PDF 및 외부 라이브러리 호환성 확보
- PDF를 `PPTX`, `DOCX`, `DOC`, `Google Slides`로 변환 가능한 서비스 구조 설계
- 상태표시줄 우측에 Git 버전 정보(`Commit Count`, `Hash 9자리`)를 표시할 수 있는 구조 확보
- OCR, 일괄 변환, 외부 API 연동이 가능한 확장형 구조 확보

## 3. 시스템 개요

프로그램은 사용자가 PDF를 선택하고 출력 포맷을 지정하면, 애플리케이션이 PDF를 분석한 뒤 지정된 포맷으로 변환하고 결과를 저장하거나 외부 서비스로 내보내는 구조로 동작한다.

구성은 크게 다음 계층으로 나눈다.

- Presentation Layer: WPF View, Style, Resource, Navigation
- Application Layer: ViewModel, Command, Use Case Orchestration
- Domain Layer: 변환 작업 모델, 결과 모델, 상태 모델
- Infrastructure Layer: PDF 파싱, OCR, 파일 저장, Google API, 로깅

## 4. 기술 스택

- Runtime: `.NET 9`
- UI: `WPF`
- Pattern: `MVVM`
- Language: `C#`
- Build Target: `x64`
- Serialization/Config: `System.Text.Json`, `appsettings.json` 또는 이에 준하는 설정 저장 구조
- Logging: `Microsoft.Extensions.Logging` 계열 또는 동급 로깅 구성
- Version Metadata: Git 기반 빌드 메타데이터 또는 런타임 Git 조회 구조

## 5. 아키텍처 설계

### 5.1 계층 구조

- `PDFConvert.App`
  - WPF 진입점, App.xaml, DI 초기화, 전역 예외 처리
- `PDFConvert.Presentation`
  - Views, ViewModels, Commands, Converters, Resources
- `PDFConvert.Domain`
  - 엔티티, 열거형, 상태 모델, 계약 인터페이스
- `PDFConvert.Application`
  - UseCase, Coordinator, 작업 흐름 제어
- `PDFConvert.Infrastructure`
  - PDF Parser, OCR Adapter, Document Exporter, Google Slides Client, File Storage, Logging
- `Tests/PDFConvert.Tests`
  - 단위 테스트 및 통합 테스트

### 5.2 MVVM 적용 원칙

- View는 UI 렌더링과 바인딩에 집중한다.
- ViewModel은 상태와 사용자 액션을 관리한다.
- 비즈니스 규칙과 변환 흐름은 서비스 또는 UseCase 계층에 둔다.
- 코드 비하인드는 최소화한다.
- 서비스는 인터페이스 기반으로 설계하여 Mock 테스트가 가능해야 한다.

## 6. 핵심 모듈 설계

### 6.1 파일 선택 모듈

책임:

- PDF 파일 선택
- 파일 유효성 검증
- 파일 메타데이터 추출

주요 구성:

- `IFileDialogService`
- `IFileValidationService`
- `PdfFileInfo`

### 6.2 변환 작업 관리 모듈

책임:

- 변환 요청 생성
- 진행 상태 업데이트
- 취소/재시도 처리

주요 구성:

- `IConversionCoordinator`
- `ConversionJob`
- `ConversionStatus`
- `ConversionHistoryItem`

### 6.3 PDF 분석 모듈

책임:

- 텍스트, 이미지, 표, 문단, 페이지 정보 추출
- OCR 필요 여부 판단

주요 구성:

- `IPdfParser`
- `IPdfLayoutAnalyzer`
- `IOcrService`
- `PdfAnalysisResult`

### 6.4 포맷 변환 모듈

책임:

- PDF 분석 결과를 포맷별 결과물로 변환

주요 구성:

- `IPptxExportService`
- `IWordExportService`
- `IDocExportService`
- `IGoogleSlidesExportService`
- `ExportRequest`
- `ExportResult`

### 6.5 저장 및 이력 모듈

책임:

- 결과 파일 저장
- 최근 작업 이력 유지
- 임시 파일 정리

주요 구성:

- `IFileStorageService`
- `IHistoryRepository`
- `ITempFileManager`

### 6.6 버전 정보 모듈

책임:

- Git 저장소 기준 버전 정보 조회
- `Commit Count`와 짧은 해시 생성
- 상태표시줄 표시용 버전 문자열 제공

주요 구성:

- `IGitVersionService`
- `GitVersionInfo`
- `StatusBarVersionPresenter`

## 7. 데이터 모델 초안

### 7.1 ConversionJob

- `JobId`
- `SourceFilePath`
- `SourceFileName`
- `TargetFormats`
- `CreatedAt`
- `StartedAt`
- `CompletedAt`
- `Status`
- `ProgressPercent`
- `ErrorCode`
- `ErrorMessage`

### 7.2 PdfAnalysisResult

- `PageCount`
- `HasScannedPages`
- `DetectedLanguage`
- `Pages`
- `Fonts`
- `Images`
- `Tables`

### 7.3 ExportResult

- `TargetFormat`
- `IsSuccess`
- `OutputPath`
- `ExternalLink`
- `WarningMessages`
- `ErrorMessage`

## 8. 화면-로직 연계 설계

### MainWindowViewModel

책임:

- 현재 선택 파일 관리
- 출력 포맷 선택 상태 관리
- 변환 시작 Command 제공
- 진행 상태 바인딩

주요 속성:

- `SelectedPdfPath`
- `SelectedFileName`
- `IsBusy`
- `ProgressValue`
- `StatusMessage`
- `CommitCount`
- `CommitHashShort`
- `VersionDisplayText`
- `SelectedOutputFormats`
- `CanStartConversion`

주요 Command:

- `BrowseFileCommand`
- `StartConversionCommand`
- `CancelConversionCommand`
- `OpenOutputFolderCommand`

### HistoryViewModel

책임:

- 최근 변환 이력 목록 제공
- 재시도 기능 제공

## 9. 변환 처리 흐름

1. 사용자가 PDF를 선택한다.
2. ViewModel이 파일 유효성 검사를 요청한다.
3. 유효한 파일이면 변환 대상 포맷을 수집한다.
4. 앱 시작 시 `IGitVersionService`가 Git 버전 정보를 조회해 상태표시줄 모델을 구성한다.
5. `IConversionCoordinator`가 작업을 생성한다.
6. PDF 분석 서비스가 문서 구조를 분석한다.
7. 필요 시 OCR을 수행한다.
8. 대상 포맷별 Export Service가 결과물을 생성한다.
9. 결과 파일을 저장하거나 Google Slides 링크를 생성한다.
10. 작업 이력을 기록하고 UI 상태를 갱신한다.

## 10. 비동기 및 스레딩 설계

- 변환 작업은 `Task` 기반 비동기로 실행한다.
- UI 바인딩 값 갱신은 UI 스레드 규칙을 준수해야 한다.
- 대용량 파일 처리 시 진행률 보고 인터페이스를 둔다.
- 취소 가능성을 고려해 `CancellationToken` 기반 구조를 적용한다.

권장 인터페이스:

- `Task<ConversionResult> ConvertAsync(ConversionJob job, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)`

## 11. 설정 및 환경 설계

관리 대상 설정:

- 기본 저장 경로
- 최근 사용 경로
- OCR 사용 여부
- Google 인증 설정
- 로그 레벨
- 최대 동시 작업 수

설정 저장 방식:

- 사용자 단위 로컬 설정 파일
- 민감 정보는 별도 보호 저장소 사용 검토

## 12. 예외 처리 설계

예외 구분:

- 파일 열기 실패
- 지원하지 않는 PDF
- 암호화 PDF
- OCR 처리 실패
- 변환 라이브러리 오류
- 파일 저장 실패
- Google API 인증 실패
- 네트워크 오류

대응 원칙:

- 사용자에게 이해 가능한 메시지 제공
- 내부 로그에 상세 원인 기록
- 가능하면 작업 전체 중단 대신 포맷별 부분 실패를 허용

## 13. 로깅 및 진단

로그 범위:

- 앱 시작/종료
- 파일 선택 및 검증
- 변환 시작/완료/실패
- 외부 API 호출 결과
- 예외 상세 정보

로그 정책:

- 개인정보나 문서 내용 원문은 로그에 저장하지 않는다.
- 파일 경로는 필요 최소 수준으로만 저장한다.

## 14. 테스트 전략

### 단위 테스트

- ViewModel Command 동작
- 파일 검증 로직
- 변환 요청 생성 로직
- 상태 전이 로직

### 통합 테스트

- PDF 분석 서비스와 변환 서비스 연계
- 결과 파일 생성 검증
- 이력 저장 동작

### 수동 테스트

- 한글 PDF
- 이미지 중심 PDF
- 표 중심 PDF
- 스캔 PDF
- 대용량 PDF
- Google Slides 연동

## 15. 외부 라이브러리 검토 포인트

- `.NET 9` 호환 여부
- `x64` 환경 지원 여부
- 상용 사용 가능 라이선스 여부
- PDF 텍스트/이미지/표 추출 정확도
- PPTX/DOCX/DOC 생성 지원 범위
- Google Slides 연동 방식 적합성

## 16. 향후 확장 고려사항

- 다중 파일 일괄 변환
- 문서 템플릿 기반 후처리
- OCR 언어팩 확장
- 변환 품질 프리셋
- 플러그인형 Export Provider 구조
