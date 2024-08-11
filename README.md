
# LocalizationManager

**LocalizationManager**는 WPF 기반의 도구로, 다국어 애플리케이션을 위한 번역 테이블을 생성 및 관리하는 기능을 제공합니다. 이 프로젝트는 개발자와 번역자가 효율적으로 협업하여 다양한 언어를 지원하는 애플리케이션을 쉽게 관리할 수 있도록 설계되었습니다.

## 📦 기능 (Features)
- 번역 테이블 생성 및 관리
- 다양한 파일 형식 지원 (CSV, JSON 등)
- 간편한 사용자 인터페이스를 통한 번역 데이터 관리
- 실시간 번역 기능 (AI 번역 API 연동)
- 다중 언어 지원 및 언어별 데이터 관리

## 🛠 개발 환경 (Development Environment)
- **프로그래밍 언어**: C#
- **프레임워크**: .NET Core 3.1, WPF (Windows Presentation Foundation)
- **IDE**: Visual Studio 2019 이상
- **필수 패키지**: NuGet 패키지로 필요한 라이브러리를 설치
  - **Newtonsoft.Json**: JSON 데이터 처리
  - **EntityFramework**: 데이터베이스 관리
  - **Log4Net**: 로깅 기능
- **운영 체제**: Windows 10 이상
- **기타 도구**: 
  - **Git**: 버전 관리
  - **GitHub**: 소스 코드 호스팅


## 🚀 설치 (Installation)
1. 이 저장소를 클론합니다:
   ```bash
   git clone https://github.com/devdeankang/LocalizationManager.git
   ```
2. 솔루션 파일 (`LocalizationManager.sln`)을 Visual Studio에서 엽니다.
3. 필요한 NuGet 패키지를 복원합니다:
   ```bash
   dotnet restore
   ```
4. 프로젝트를 빌드하고 실행합니다.

## 🛠 사용 방법 (Usage)
1. 애플리케이션을 실행한 후, 새 번역 테이블을 생성하거나 기존 테이블을 불러옵니다.
2. 번역할 텍스트를 입력하거나 파일에서 가져옵니다.
3. 필요에 따라 AI 번역 기능을 사용하여 자동 번역을 수행합니다.
4. 번역 테이블을 저장하고 필요한 파일 형식으로 내보냅니다.

## 🌟 기여 (Contributing)
기여를 환영합니다! 개선 사항이나 버그가 있으면 이슈를 등록해 주시고, 풀 리퀘스트를 보내주세요.

1. 이 저장소를 포크합니다.
2. 새로운 브랜치를 만듭니다:
   ```bash
   git checkout -b feature/FeatureName
   ```
3. 변경 사항을 커밋합니다:
   ```bash
   git commit -m 'Add some FeatureName'
   ```
4. 브랜치에 푸시합니다:
   ```bash
   git push origin feature/FeatureName
   ```
5. 풀 리퀘스트를 생성합니다.

## 📄 라이선스 (License)
이 프로젝트는 MIT 라이선스에 따라 라이선스가 부여됩니다. 자세한 내용은 `LICENSE` 파일을 참조하세요.
