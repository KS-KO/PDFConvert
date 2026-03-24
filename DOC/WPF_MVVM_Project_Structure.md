# PDF to Office/Slides Converter WPF MVVM 프로젝트 구조 초안

## 1. 문서 개요

- 문서명: WPF MVVM 프로젝트 구조 초안
- 버전: v0.2
- 작성일: 2026-03-24
- 기준 문서: `PRD_PDF_Converter.md`, `Technical_Design_PDF_Converter.md`

## 2. 목표

- `.NET 9`, `x64`, `WPF`, `MVVM` 요구사항에 맞는 기본 솔루션 구조 제안
- UI, 비즈니스 로직, 인프라 로직의 책임 분리
- 테스트 가능한 구조 확보
- Git 버전 정보(`Commit Count`, `Hash 9자리`)를 상태표시줄 우측에 표시할 수 있는 구조 반영
- 이후 OCR, Google Slides, 일괄 변환 기능 확장 가능성 확보

## 3. 권장 솔루션 구조

```text
PDFConvert.sln
src/
  PDFConvert.App/
  PDFConvert.Presentation/
  PDFConvert.Application/
  PDFConvert.Domain/
  PDFConvert.Infrastructure/
Tests/
  PDFConvert.Tests/
docs/
  architecture/
  ui/
```

## 4. 프로젝트별 역할

### PDFConvert.App

역할:

- WPF 앱 진입점
- `App.xaml`, `App.xaml.cs`
- DI 컨테이너 초기화
- 전역 예외 처리
- 메인 윈도우 시작

주요 파일 예시:

- `App.xaml`
- `App.xaml.cs`
- `Bootstrapper.cs`
- `ServiceCollectionExtensions.cs`

### PDFConvert.Presentation

역할:

- View, ViewModel, Command, Converter, Resource 관리
- 사용자 입력 및 UI 상태 제어
- 상태표시줄 버전 정보 표시 데이터 관리

하위 구조 예시:

```text
PDFConvert.Presentation/
  Views/
    MainWindow.xaml
    MainWindow.xaml.cs
    HistoryView.xaml
    SettingsView.xaml
    Dialogs/
  ViewModels/
    MainWindowViewModel.cs
    ConversionProgressViewModel.cs
    ConversionResultViewModel.cs
    HistoryViewModel.cs
    SettingsViewModel.cs
    GoogleAuthViewModel.cs
    StatusBarViewModel.cs
  Commands/
    AsyncRelayCommand.cs
    RelayCommand.cs
  Converters/
    BooleanToVisibilityConverter.cs
    EnumToBooleanConverter.cs
  Resources/
    Styles/
    Templates/
    Brushes.xaml
    Typography.xaml
```

### PDFConvert.Application

역할:

- 유스케이스 조합
- 작업 흐름 제어
- 서비스 오케스트레이션
- Git 버전 정보 제공

하위 구조 예시:

```text
PDFConvert.Application/
  Interfaces/
    IConversionCoordinator.cs
    IGitVersionService.cs
    IHistoryService.cs
    ISettingsService.cs
  UseCases/
    StartConversionUseCase.cs
    RetryConversionUseCase.cs
    LoadHistoryUseCase.cs
  DTOs/
    ConversionRequestDto.cs
    ConversionResultDto.cs
    GitVersionInfoDto.cs
  Services/
    ConversionCoordinator.cs
    GitVersionService.cs
```

### PDFConvert.Domain

역할:

- 핵심 모델과 규칙 정의

하위 구조 예시:

```text
PDFConvert.Domain/
  Entities/
    ConversionJob.cs
    ConversionResult.cs
    PdfFileInfo.cs
    HistoryItem.cs
  Enums/
    ConversionStatus.cs
    OutputFormat.cs
    ErrorCode.cs
  ValueObjects/
    FileMetadata.cs
    ProgressInfo.cs
  Interfaces/
    IPdfParser.cs
    IOcrService.cs
    IWordExportService.cs
    IPptxExportService.cs
    IDocExportService.cs
    IGoogleSlidesExportService.cs
    IFileStorageService.cs
```

### PDFConvert.Infrastructure

역할:

- 외부 라이브러리 및 시스템 리소스 연동
- Git 저장소 메타데이터 조회

하위 구조 예시:

```text
PDFConvert.Infrastructure/
  Pdf/
    PdfParser.cs
    PdfLayoutAnalyzer.cs
  Ocr/
    OcrService.cs
  Export/
    PptxExportService.cs
    WordExportService.cs
    DocExportService.cs
    GoogleSlidesExportService.cs
  Storage/
    FileStorageService.cs
    HistoryRepository.cs
    TempFileManager.cs
  Auth/
    GoogleAuthService.cs
  Logging/
    AppLogger.cs
  Configuration/
    AppSettingsRepository.cs
  Versioning/
    GitVersionReader.cs
```

### PDFConvert.Tests

역할:

- 단위 테스트
- 통합 테스트

하위 구조 예시:

```text
PDFConvert.Tests/
  ViewModels/
  Application/
  Domain/
  Infrastructure/
  TestData/
```

## 5. 네이밍 원칙

- ViewModel은 `...ViewModel`
- Service는 역할 중심 `...Service`
- UseCase는 동사 중심 `...UseCase`
- 인터페이스는 `I` 접두사 사용
- 상태값은 Enum으로 통일

## 6. 빌드 및 타겟 정책

- Target Framework: `net9.0-windows`
- Platform Target: `x64`
- Nullable: `enable`
- Implicit Usings: `enable`
- WPF: `UseWPF=true`

권장 설정 예시:

```xml
<PropertyGroup>
  <TargetFramework>net9.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
  <Platforms>x64</Platforms>
  <PlatformTarget>x64</PlatformTarget>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

## 7. DI 구성 원칙

- ViewModel은 생성자 주입 사용
- 서비스는 인터페이스 기반 등록
- 외부 API/설정/로깅 서비스는 싱글턴 또는 적절한 수명주기 사용
- 변환 작업 오케스트레이션은 Application 계층에서 조립

예시 등록 범위:

- Singleton
  - 설정 서비스
  - 로깅 서비스
  - Google 인증 상태 서비스
- Transient
  - ViewModel
  - UseCase
- Scoped 또는 별도 관리
  - 장시간 실행 변환 세션 객체

## 8. MVVM 구현 규칙

- `INotifyPropertyChanged` 기반 공통 베이스 클래스 사용
- 비동기 명령은 `AsyncRelayCommand` 사용
- 다이얼로그 호출은 직접 View에 의존하지 않고 서비스로 추상화
- 파일 선택, 폴더 열기, 메시지 박스는 UI 서비스로 분리
- 상태표시줄의 버전 정보는 서비스 또는 별도 ViewModel을 통해 주입

권장 공통 클래스:

- `ObservableObject`
- `ViewModelBase`
- `RelayCommand`
- `AsyncRelayCommand`

## 9. 리소스 구조

WPF 리소스 분리 예시:

```text
Resources/
  Styles/
    Buttons.xaml
    Inputs.xaml
    Panels.xaml
  Templates/
    ConversionResultTemplate.xaml
    HistoryItemTemplate.xaml
  Themes/
    LightTheme.xaml
  Brushes.xaml
  Icons.xaml
  Typography.xaml
```

## 10. 설정 및 저장 파일 위치 예시

- 사용자 설정: `%AppData%` 또는 `%LocalAppData%`
- 로그 파일: 앱 전용 로그 디렉터리
- 임시 변환 파일: 사용자 로컬 임시 경로
- 최근 이력: JSON 또는 SQLite 기반 저장 검토

## 11. 초기 구현 우선순위

1. 솔루션 및 프로젝트 구조 생성
2. 메인 화면과 MainWindowViewModel 구성
3. 파일 선택 및 기본 검증 구현
4. 변환 오케스트레이터 인터페이스 구성
5. PPTX/DOCX MVP 변환 흐름 연결
6. 결과 저장 및 상태 표시
7. 이력 및 설정 화면 추가
8. OCR 및 Google Slides 확장

## 12. 권장 폴더 트리 상세 예시

```text
src/
  PDFConvert.App/
    App.xaml
    App.xaml.cs
    Bootstrapper.cs
  PDFConvert.Presentation/
    Views/
    ViewModels/
    Commands/
    Converters/
    Resources/
    Services/
  PDFConvert.Application/
    Interfaces/
    UseCases/
    DTOs/
    Services/
  PDFConvert.Domain/
    Entities/
    Enums/
    Interfaces/
    ValueObjects/
  PDFConvert.Infrastructure/
    Pdf/
    Ocr/
    Export/
    Storage/
    Auth/
    Logging/
    Configuration/
Tests/
  PDFConvert.Tests/
```

## 13. 확장 포인트

- Export Provider를 추가해 새로운 출력 포맷 지원
- OCR 엔진 구현 교체
- 저장소 구현을 JSON에서 DB로 전환
- Google Slides 외 추가 클라우드 문서 서비스 연동

## 14. 권장 다음 단계

- 솔루션 생성 및 프로젝트 스캐폴딩
- 공통 MVVM 베이스 클래스 작성
- 메인 화면 XAML 및 ViewModel 초안 작성
- 변환 서비스 인터페이스 정의
- MVP 대상 포맷 우선 구현
