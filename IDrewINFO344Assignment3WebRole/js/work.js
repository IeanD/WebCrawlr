'use strict';

var urlInputForm = document.getElementById("urlInputForm");
urlInputForm.addEventListener("submit", function (e) {
    performSearch(e)
});

function performSearch(e) {
    e.preventDefault();

    let input = document.getElementById("urlCrawl").value;
    const url = "services/WebCrawler.asmx/StartCrawling";
    let data = JSON.stringify({
        website: input
    });
    let init = {
        method: 'POST',
        body: data,
        headers: new Headers({
            'content-type': 'application/json'
        })
    };
    fetch(url, init)
        .then(function (response) {
            return response.json();
        }).then(function (json) {
            console.log(json.d);
        }
    );
}
