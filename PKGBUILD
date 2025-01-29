# Maintainer: pitman2e
pkgname=hash-calculator
pkgver=1.2.0
pkgrel=2
pkgdesc="A file hash calculator to verify file integrity"
arch=('any')
depends=("dotnet-runtime")
makedepends=("dotnet-sdk" "git")
conflicts=()
provides=()
source=("git-src::git+https://github.com/pitman2e/HashCalculator.git")
#source=("git-src::git+file://$(pwd)/.git")
sha512sums=("SKIP")

build() {
  cd "${srcdir}/git-src/HashCalculator"
  dotnet publish
}

package() {
  install -D "${srcdir}/git-src/HashCalculator/bin/Release/net9.0/publish"/* -t "${pkgdir}/usr/share/hash-calculator"
  mkdir "${pkgdir}/usr/bin/"
  ln -s "${pkgdir}/usr/share/hash-calculator/HashCalculator" "${pkgdir}/usr/bin/hash-calculator"
}
