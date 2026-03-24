# PDF to Office/Slides Converter 화면정의서

## 1. 문서 개요

- 문서명: PDF to Office/Slides Converter 화면정의서
- 버전: v0.1
- 작성일: 2026-03-24
- 기준 문서: `PRD_PDF_Converter.md`, `Technical_Design_PDF_Converter.md`

## 2. 화면 구성 원칙

- 데스크톱 사용성을 우선으로 한 단순한 1차 플로우 제공
- WPF 바인딩 중심 구조에 적합한 화면 상태 분리
- 사용자가 현재 단계와 다음 액션을 쉽게 이해할 수 있는 정보 구조 유지
- 장시간 변환 작업에서도 앱이 멈춘 것처럼 보이지 않도록 상태 피드백 강화

## 3. 화면 목록

- `SCR-001` 메인 화면
- `SCR-002` 변환 진행 팝업 또는 진행 패널
- `SCR-003` 변환 결과 화면 또는 결과 패널
- `SCR-004` 이력 화면
- `SCR-005` 설정 화면
- `SCR-006` Google 인증 화면
- `SCR-007` 오류/경고 다이얼로그

## 4. SCR-001 메인 화면

### 목적

- PDF 선택
- 출력 포맷 선택
- 변환 시작

### 주요 영역

- 상단: 앱 제목, 설정 버튼, 이력 버튼
- 중앙 좌측: 파일 선택 영역
- 중앙 우측: 출력 형식 선택 영역
- 하단: 변환 시작 버튼, 상태 메시지

### 주요 UI 요소

- `Button`: PDF 선택
- `TextBlock`: 선택 파일명
- `TextBlock`: 파일 크기, 페이지 수, 암호화 여부
- `CheckBox`: PPTX
- `CheckBox`: DOCX
- `CheckBox`: DOC
- `CheckBox`: Google Slides
- `CheckBox`: OCR 사용
- `Button`: 변환 시작
- `Button`: 취소
- `ProgressBar`: 전체 진행률
- `TextBlock`: 상태 메시지

### 상태 정의

- 초기 상태
  - 파일 미선택
  - 변환 시작 버튼 비활성화
- 파일 선택 완료
  - 파일 정보 표시
  - 출력 형식 선택 가능
- 변환 진행 중
  - 파일 선택 및 형식 선택 비활성화
  - 진행률 및 상태 메시지 활성화
- 변환 완료
  - 결과 열기/저장 버튼 노출
- 변환 실패
  - 오류 메시지 및 재시도 유도

### 사용자 액션

- PDF 파일 선택
- 출력 포맷 선택
- OCR 사용 여부 변경
- 변환 시작
- 작업 취소

## 5. SCR-002 변환 진행 화면

### 목적

- 현재 진행 단계와 예상 흐름 안내

### 주요 UI 요소

- `ProgressBar`: 진행률
- `ItemsControl` 또는 `ListBox`: 단계별 상태 목록
- `TextBlock`: 현재 처리 중인 단계
- `Button`: 취소

### 표시 단계 예시

- 파일 검증 중
- PDF 분석 중
- OCR 처리 중
- PPTX 생성 중
- DOCX 생성 중
- Google Slides 업로드 중
- 완료 처리 중

### 상태 규칙

- 단계 완료 시 완료 표시
- 실패 단계 발생 시 원인 메시지 노출
- 부분 성공 시 성공/실패 포맷을 구분 표시

## 6. SCR-003 결과 화면

### 목적

- 변환 결과 확인 및 후속 액션 제공

### 주요 UI 요소

- `ListView`: 출력 형식별 결과 목록
- `TextBlock`: 성공/실패 상태
- `Button`: 파일 열기
- `Button`: 폴더 열기
- `Button`: 링크 열기
- `Button`: 다시 변환

### 표시 정보

- 출력 형식
- 결과 상태
- 저장 경로
- 외부 링크
- 경고 메시지

## 7. SCR-004 이력 화면

### 목적

- 최근 변환 작업 조회
- 재시도 및 결과 재열기

### 주요 UI 요소

- `DataGrid` 또는 `ListView`: 작업 이력
- `TextBox`: 검색
- `ComboBox`: 상태 필터
- `Button`: 재시도
- `Button`: 결과 열기
- `Button`: 삭제

### 표시 컬럼

- 작업일시
- 원본 파일명
- 출력 형식
- 상태
- 저장 위치

## 8. SCR-005 설정 화면

### 목적

- 기본 환경 설정 관리

### 주요 UI 요소

- `TextBox`: 기본 저장 경로
- `Button`: 경로 선택
- `CheckBox`: OCR 기본 사용
- `CheckBox`: 완료 후 폴더 자동 열기
- `ComboBox`: 로그 레벨
- `TextBox` 또는 `NumericUpDown`: 최대 동시 작업 수
- `Button`: 저장
- `Button`: 취소

## 9. SCR-006 Google 인증 화면

### 목적

- Google Slides 연동을 위한 인증 및 상태 안내

### 주요 UI 요소

- `TextBlock`: 현재 로그인 상태
- `Button`: 로그인
- `Button`: 로그아웃
- `TextBlock`: 권한 안내
- `TextBlock`: 인증 만료 여부

### 예외 메시지

- 인증 실패
- 권한 부족
- 네트워크 오류

## 10. SCR-007 오류/경고 다이얼로그

### 목적

- 실패 원인 및 대응 방법 안내

### 유형

- 파일 형식 오류
- 암호화 PDF
- 저장 실패
- 네트워크 오류
- Google 인증 오류
- OCR 실패

### 공통 요소

- 제목
- 요약 메시지
- 상세 보기 버튼
- 확인 버튼
- 재시도 버튼

## 11. 화면 전이

1. 앱 시작 시 `SCR-001` 진입
2. 이력 버튼 클릭 시 `SCR-004` 이동 또는 패널 전환
3. 설정 버튼 클릭 시 `SCR-005` 이동 또는 다이얼로그 표시
4. Google Slides 선택 후 인증 필요 시 `SCR-006` 표시
5. 변환 시작 후 `SCR-002` 상태 표시
6. 변환 종료 후 `SCR-003` 결과 표시
7. 오류 발생 시 `SCR-007` 표시

## 12. ViewModel 매핑 초안

- `SCR-001` -> `MainWindowViewModel`
- `SCR-002` -> `ConversionProgressViewModel`
- `SCR-003` -> `ConversionResultViewModel`
- `SCR-004` -> `HistoryViewModel`
- `SCR-005` -> `SettingsViewModel`
- `SCR-006` -> `GoogleAuthViewModel`
- `SCR-007` -> `DialogViewModel` 또는 공통 Dialog Service

## 13. 바인딩 항목 예시

### MainWindowViewModel

- `SelectedPdfPath`
- `SelectedFileName`
- `SelectedOutputFormats`
- `UseOcr`
- `IsBusy`
- `ProgressValue`
- `StatusMessage`

### ConversionResultViewModel

- `Results`
- `HasAnySuccess`
- `HasAnyFailure`

### HistoryViewModel

- `HistoryItems`
- `SelectedHistoryItem`
- `FilterStatus`

## 14. UX 유의사항

- 메인 화면에서 가장 중요한 액션은 `PDF 선택`과 `변환 시작`이어야 한다.
- Google Slides는 외부 연동 기능이므로 별도 안내를 명확히 보여준다.
- 실패 메시지는 기술 용어보다 행동 지침 중심으로 제공한다.
- 긴 작업의 경우 현재 단계가 무엇인지 반드시 보여준다.
- 한글 문서 사용성이 중요하므로 기본 폰트, 간격, 대비를 안정적으로 설계한다.
